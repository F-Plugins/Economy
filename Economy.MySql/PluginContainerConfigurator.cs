using OpenMod.API.Plugins;
using OpenMod.EntityFrameworkCore.MySql.Extensions;

namespace Economy.MySql;

public class PluginContainerConfigurator : IPluginContainerConfigurator
{
    public void ConfigureContainer(IPluginServiceConfigurationContext context)
    {
        context.ContainerBuilder.AddMySqlDbContext<EconomyDbContext>();
    }
}