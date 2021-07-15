using Cysharp.Threading.Tasks;
using ExperienceProvider.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExperienceProvider
{
    [ServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Highest)]
    public class EconomyProvider : EconomyProviderCore, IEconomyProvider, IDisposable
    {
        public string CurrencyName => configuration.GetSection("CurrencyConfiguration:CurrencyName").Get<string>();

        public string CurrencySymbol => configuration.GetSection("CurrencyConfiguration:CurrencySymbol").Get<string>();

        public async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
        {
            await EnsureAccountIsCreated(ownerId, ownerType);

            return data.FirstOrDefault(x => x.UserId == ownerId && x.UserType == ownerType).Balance;
        }

        public async Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
        {
            await EnsureAccountIsCreated(ownerId, ownerType);

            var oldBalance = data.FirstOrDefault(x => x.UserId == ownerId && x.UserType == ownerType).Balance;

            data.FirstOrDefault(x => x.UserId == ownerId && x.UserType == ownerType).Balance = balance;

            await SetUserExperience(ownerId, ownerType, balance);

            var @event = new BalanceUpdatedEvent(ownerId, ownerType, oldBalance, balance, String.Empty);

            await eventBus.EmitAsync(openModComponent, this, @event);
        }

        public async Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal changeAmount, string reason)
        {
            await EnsureAccountIsCreated(ownerId, ownerType);

            var oldBalance = data.FirstOrDefault(x => x.UserId == ownerId && x.UserType == ownerType).Balance;
            var newBalance = oldBalance + changeAmount;

            if (newBalance < 0)
                throw new NotEnoughBalanceException("Fatal Error Not EnoughBalance");

            data.FirstOrDefault(x => x.UserId == ownerId && x.UserType == ownerType).Balance = newBalance;

            await SetUserExperience(ownerId, ownerType, newBalance);

            var @event = new BalanceUpdatedEvent(ownerId, ownerType, oldBalance, newBalance, reason);

            await eventBus.EmitAsync(openModComponent, this, @event);

            return newBalance;
        }

        protected override async Task StartAsync()
        {
            if (!await dataStore.ExistsAsync("accounts"))
            {
                data = new List<Account>();
            }
            else
            {
                data = await dataStore.LoadAsync<List<Account>>("accounts");
            }

            SaveManager.onPostSave += OnPostSave;
        }

        private Task EnsureAccountIsCreated(string userId, string userType)
        {
            if (!data.Any(x => x.UserId == userId && x.UserType == userType))
            {
                data.Add(new Account
                {
                    UserId = userId,
                    UserType = userType,
                    Balance = configuration.GetSection("BalanceConfiguration:DefaultBalance").Get<decimal>()
                });
            }

            return Task.CompletedTask;
        }

        private async UniTask SetUserExperience(string userId, string userType, decimal balance)
        {
            var find = await userManager.FindUserAsync(userType, userId, UserSearchMode.FindById);

            if(find is UnturnedUser user && user.Session != null)
            {
                await UniTask.SwitchToMainThread();
                user.Player.Player.skills.ServerSetExperience((uint)balance);
                await UniTask.SwitchToThreadPool();
            }
        }

        private async void OnPostSave()
        {
            await dataStore.SaveAsync("accounts", data);
        }

        public void Dispose()
        {
            SaveManager.onPostSave -= OnPostSave;
        }        
    }
}
