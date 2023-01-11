using Autofac;
using Economy.API;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.API.Plugins;
using OpenMod.API.Prioritization;

namespace Economy.MySql;

[ServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Lowest)]
public class AccountsRepository : IAccountsRepository
{
    private readonly IPluginAccessor<EconomyMySqlPlugin> _pluginAccessor;

    public AccountsRepository(IPluginAccessor<EconomyMySqlPlugin> pluginAccessor)
    {
        _pluginAccessor = pluginAccessor;
    }
    
    public async Task<Account?> GetAsync(string ownerId, string ownerType)
    {
        await using var context = GetDbContext();
        return await context.Accounts.FindAsync(ownerId, ownerType);
    }

    public async Task UpsertAsync(Account account)
    {
        await using var context = GetDbContext();
        if (await context.Accounts.AnyAsync(a => a.OwnerId == account.OwnerId && a.OwnerType == account.OwnerType))
            context.Accounts.Update(account);
        else
            await context.Accounts.AddAsync(account);
        await context.SaveChangesAsync();
    }

    private EconomyDbContext GetDbContext() => _pluginAccessor.Instance!.LifetimeScope.Resolve<EconomyDbContext>();
}