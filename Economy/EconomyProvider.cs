using Cysharp.Threading.Tasks;
using Economy.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMod.API;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Users;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Players.Skills.Events;
using OpenMod.Unturned.Users;
using OpenMod.Unturned.Users.Events;
using SilK.Unturned.Extras.Configuration;
using SilK.Unturned.Extras.Dispatcher;
using SilK.Unturned.Extras.Events;
using SilK.Unturned.Extras.Localization;

namespace Economy;

[ServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.High)]
internal class EconomyProvider : IEconomyProvider,
    IInstanceEventListener<UnturnedUserConnectedEvent>,
    IInstanceEventListener<UnturnedPlayerExperienceUpdatedEvent>,
    IInstanceEventListener<BalanceUpdatedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly IRuntime _runtime;
    private readonly IStringLocalizerAccessor<EconomyPlugin> _stringLocalizer;
    private readonly IConfigurationAccessor<EconomyPlugin> _configuration;
    private readonly IAccountsRepository _accountsRepository;
    private readonly IUnturnedUserDirectory _unturnedUserDirectory;
    private readonly ILogger<EconomyProvider> _logger;
    private readonly IActionDispatcher _actionDispatcher;

    public EconomyProvider(
        IEventBus eventBus,
        IRuntime runtime,
        IStringLocalizerAccessor<EconomyPlugin> stringLocalizer,
        IConfigurationAccessor<EconomyPlugin> configuration,
        IAccountsRepository accountsRepository,
        IUnturnedUserDirectory unturnedUserDirectory,
        ILogger<EconomyProvider> logger,
        IActionDispatcher actionDispatcher)
    {
        _eventBus = eventBus;
        _runtime = runtime;
        _stringLocalizer = stringLocalizer;
        _configuration = configuration;
        _accountsRepository = accountsRepository;
        _unturnedUserDirectory = unturnedUserDirectory;
        _logger = logger;
        _actionDispatcher = actionDispatcher;
    }

    public string CurrencyName => _stringLocalizer["economy:currency:name"];
    public string CurrencySymbol => _stringLocalizer["economy:currency:symbol"];

    public bool UseXp => _configuration.GetSection("economy:use_xp").Get<bool>();

    public async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
    {
        if (ownerType == KnownActorTypes.Player && UseXp)
        {
            var user = FindUser(ownerId);
            if (user is not null)
                return user.Player.Player.skills.experience;
        }

        var account = await GetAccountAsync(ownerId, ownerType);
        return account.Balance;
    }

    public async Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
    {
        if (!_configuration.GetSection("economy:allow_setting_negative_balance").Get<bool>() && balance < 0)
            balance = 0;

        if (ownerType == KnownActorTypes.Player && UseXp)
        {
            balance = Math.Truncate(balance);

            var user = FindUser(ownerId);
            if (user is not null)
            {
                if ((uint)balance == user.Player.Player.skills.experience)
                    await UpdateExperience(user, balance);
            }
        }

        var account = await GetAccountAsync(ownerId, ownerType);
        var oldBalance = account.Balance;
        account.Balance = balance;
        await _accountsRepository.UpsertAsync(account);

        await EmitBalanceUpdatedEventAsync(ownerId, ownerType, oldBalance, balance);
    }

    public async Task<decimal> UpdateBalanceAsync(string ownerId, string ownerType, decimal changeAmount,
        string? reason)
    {
        var account = await GetAccountAsync(ownerId, ownerType);

        var oldBalance = ownerType == KnownActorTypes.Player && UseXp ? await GetBalanceAsync(ownerId, ownerType) : account.Balance;
        var newBalance = oldBalance + changeAmount;

        if (newBalance < 0)
        {
            throw new NotEnoughBalanceException(
                _stringLocalizer["economy:errors:not_enough_balance",
                    new { CurrencySymbol, Balance = oldBalance, Amount = changeAmount }], oldBalance);
        }

        if (ownerType == KnownActorTypes.Player && UseXp)
        {
            newBalance = Math.Truncate(newBalance);

            var user = FindUser(ownerId);
            if (user is not null)
                await UpdateExperience(user, newBalance);
        }

        account.Balance = newBalance;
        await _accountsRepository.UpsertAsync(account);
        await EmitBalanceUpdatedEventAsync(ownerId, ownerType, oldBalance, newBalance, reason);
        return newBalance;
    }

    public async UniTask HandleEventAsync(object? sender, UnturnedUserConnectedEvent @event)
    {
        var account = await GetAccountAsync(@event.User.Id, @event.User.Type);
        if (account.Balance != @event.User.Player.Player.skills.experience)
        {
            UniTask.Run(async () =>
            {
                await UniTask.WaitForEndOfFrame();
                await UpdateExperience(@event.User, account.Balance);
            }).Forget(e => _logger.LogError(e, "There was an error while setting the player experience"));
        }
    }

    private async Task<Account> GetAccountAsync(string ownerId, string ownerType)
    {
        return await _accountsRepository.GetAsync(ownerId, ownerType) ?? new Account(ownerId, ownerType,
            _configuration.GetSection("economy:initial_balance").Get<decimal>());
    }

    private async Task EmitBalanceUpdatedEventAsync(string ownerId, string ownerType, decimal oldBalance, decimal newBalance, string? reason = "")
    {
        var @event = new BalanceUpdatedEvent(ownerId, ownerType, oldBalance, newBalance, reason);
        await _eventBus.EmitAsync(_runtime, this, @event);
    }

    private async UniTask UpdateExperience(UnturnedUser user, decimal amount)
    {
        await UniTask.SwitchToMainThread();

        user.Player.Player.skills.ServerSetExperience((uint)amount);
    }

    private UnturnedUser? FindUser(string userId)
    {
        return _unturnedUserDirectory.FindUser(userId, UserSearchMode.FindById);
    }

    public UniTask HandleEventAsync(object? sender, UnturnedPlayerExperienceUpdatedEvent @event)
    {
        Task.Run(() => _actionDispatcher.Enqueue(async () =>
        {
            var account = await GetAccountAsync(@event.Player.SteamId.ToString(), KnownActorTypes.Player);

            var experience = @event.Player.Player.skills.experience;

            if (account.Balance != experience)
            {
                _logger.LogDebug("Account {SteamId} balance is not sync with the player experience. Balance: {Balance} Experience: {experience}", @event.Player.SteamId, account.Balance, experience);
                await SetBalanceAsync(@event.Player.SteamId.ToString(), KnownActorTypes.Player, experience);
            }
            else
            {
                _logger.LogDebug("Account {SteamId} balance is sync with the player experience. Balance: {Balance} Experience: {experience}", @event.Player.SteamId, account.Balance, experience);
            }
        }));
        return UniTask.CompletedTask;
    }

    public UniTask HandleEventAsync(object? sender, BalanceUpdatedEvent @event)
    {
        if (@event.OwnerType != KnownActorTypes.Player || !UseXp)
            return UniTask.CompletedTask;

        var user = FindUser(@event.OwnerId);

        if (user is null)
            return UniTask.CompletedTask;

        var experience = user.Player.Player.skills.experience;

        if (@event.NewBalance != experience)
        {
            _logger.LogDebug("Experience {OwnerId} is not sync with the player balance. Balance: {NewBalance} Experience: {experience}", @event.OwnerId, @event.NewBalance, experience);
        }
        else
        {
            _logger.LogDebug("Experience {OwnerId} is sync with the player balance. Balance: {NewBalance} Experience: {experience}", @event.OwnerId, @event.NewBalance, experience);
        }

        return UniTask.CompletedTask;
    }
}