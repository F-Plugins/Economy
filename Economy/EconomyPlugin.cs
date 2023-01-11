using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.Unturned.Plugins;

[assembly: PluginMetadata("Feli.Economy", DisplayName = "Economy", Author = "Feli", Website = "docs.fplugins.com")]

namespace Economy;

internal class EconomyPlugin : OpenModUnturnedPlugin
{
    public EconomyPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override UniTask OnLoadAsync()
    {
        Logger.LogInformation("If you have problems while installing this plugin please refer to: https://docs.fplugins.com/plugins/economy.md or discord.fplugins.com");
        return base.OnLoadAsync();
    }
}