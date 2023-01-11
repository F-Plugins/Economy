using OpenMod.API.Plugins;
using OpenMod.Unturned.Plugins;

[assembly: PluginMetadata("Feli.Economy", DisplayName = "Economy", Author = "Feli", Website = "docs.fplugins.com")]

namespace Economy;

internal class EconomyPlugin : OpenModUnturnedPlugin
{
    public EconomyPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}