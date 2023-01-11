using Economy.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Prioritization;
using OpenMod.Extensions.Economy.Abstractions;
using SilK.Unturned.Extras.Configuration;
using SilK.Unturned.Extras.Localization;

namespace Economy;

[ServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.High)]
internal class EconomyProvider : IEconomyProvider
{   
    private readonly IEventBus _eventBus;
    private readonly IRuntime _runtime;
    private readonly IStringLocalizerAccessor<EconomyPlugin> _stringLocalizer;
    private readonly IConfigurationAccessor<EconomyPlugin> _configuration;
    private readonly IAccountsRepository _accountsRepository;

    public EconomyProvider(
        IEventBus eventBus,
        IRuntime runtime,
        IStringLocalizerAccessor<EconomyPlugin> stringLocalizer,
        IConfigurationAccessor<EconomyPlugin> configuration,
        IAccountsRepository accountsRepository)
    {
        _eventBus = eventBus;
        _runtime = runtime;
        _stringLocalizer = stringLocalizer;
        _configuration = configuration;
        _accountsRepository = accountsRepository;
    }
    
    public string CurrencyName => _stringLocalizer["economy:currency:name"];
    public string CurrencySymbol => _stringLocalizer["economy:currency:symbol"];

    public async Task<decimal> GetBalanceAsync(string ownerId, string ownerType)
    {
        var account = await GetAccountAsync(ownerId, ownerType);
        return account.Balance;
    }

    public async Task SetBalanceAsync(string ownerId, string ownerType, decimal balance)
    {
        if (!_configuration.GetSection("economy:allow_setting_negative_balance").Get<bool>() && balance < 0)
            balance = 0;

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

        var oldBalance = account.Balance;
        var newBalance = oldBalance + changeAmount;

        if (newBalance < 0)
        {
            throw new NotEnoughBalanceException(
                _stringLocalizer["economy:errors:not_enough_balance",
                    new { CurrencySymbol, Balance = oldBalance, Amount = changeAmount }], oldBalance);
        }

        account.Balance = newBalance;
        await _accountsRepository.UpsertAsync(account);
        await EmitBalanceUpdatedEventAsync(ownerId, ownerType, oldBalance, newBalance, reason);
        return newBalance;
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
}