using OpenMod.API.Plugins;
using OpenMod.Unturned.Plugins;

[assembly: PluginMetadata("Feli.Economy.LiteDB", DisplayName = "Economy LiteDB Repositories", Author = "Feli", Website = "docs.fplugins.com")]

namespace Economy.LiteDB;

public class EconomyLiteDBPlugin : OpenModUnturnedPlugin
{
    public EconomyLiteDBPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}