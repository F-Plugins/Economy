using Economy.API;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.API.Plugins;
using OpenMod.API.Prioritization;
using SilK.Unturned.Extras.Dispatcher;

namespace Economy.LiteDB;

[ServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Lowest)]
public class AccountsRepository : IAccountsRepository
{
    private readonly IPluginAccessor<EconomyLiteDBPlugin> _pluginAccessor;
    private readonly IActionDispatcher _dispatcher;
    
    public AccountsRepository(IPluginAccessor<EconomyLiteDBPlugin> pluginAccessor, IActionDispatcher dispatcher)
    {
        _pluginAccessor = pluginAccessor;
        _dispatcher = dispatcher;
    }

    public async Task<Account?> GetAsync(string ownerId, string ownerType)
    {
        // composite primary key does not exists in litedb 
        var accountId = ownerId + "-" + ownerType;

        return await _dispatcher.Enqueue(() =>
        {
            using var database = GetDatabase();
            var accounts = database.GetCollection("accounts");
            var account = accounts.FindById(accountId);
            return account is null ? null : new Account(ownerId, ownerType, account["balance"]);
        });
    }

    public async Task UpsertAsync(Account account)
    {
        // composite primary key does not exists in litedb 
        var accountId = account.OwnerId + "-" + account.OwnerType;

        await _dispatcher.Enqueue(() =>
        {
            using var database = GetDatabase();
            var accounts = database.GetCollection("accounts");
            var document = new BsonDocument
            {
                ["_id"] = accountId,
                ["balance"] = account.Balance
            };
            accounts.Upsert(document);
        });
    }
    
    private ILiteDatabase GetDatabase() => new LiteDatabase(Path.Combine(_pluginAccessor.Instance!.WorkingDirectory, "economy.db"));
}