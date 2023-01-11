using Autofac;
using Cysharp.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenMod.API.Plugins;
using OpenMod.Unturned.Plugins;

[assembly: PluginMetadata("Feli.Economy.MySql", DisplayName = "Economy MySql Repositories", Author = "Feli", Website = "docs.fplugins.com")]

namespace Economy.MySql;

public class EconomyMySqlPlugin : OpenModUnturnedPlugin
{
    public EconomyMySqlPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override async UniTask OnLoadAsync()
    {
        await LifetimeScope.Resolve<EconomyDbContext>().Database.MigrateAsync();
    }
}