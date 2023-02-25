using OpenMod.API.Eventing;
using OpenMod.Core.Plugins.Events;
using SilK.Unturned.Extras;
using SilK.Unturned.Extras.Events;

namespace Economy.Events;

internal class PluginLoadedEventListener : IEventListener<PluginLoadedEvent>
{
    private readonly IEventSubscriber _eventSubscriber;
    private readonly EconomyPlugin _economyPlugin;
        
    public PluginLoadedEventListener(IEventSubscriber eventSubscriber, EconomyPlugin economyPlugin)
    {
        _economyPlugin = economyPlugin;
        _eventSubscriber = eventSubscriber;
    }
        
    public Task HandleEventAsync(object? sender, PluginLoadedEvent @event)
    {
        if (@event.Plugin.GetType() != typeof(UnturnedExtrasPlugin))
            return Task.CompletedTask;

        _eventSubscriber.Subscribe(_economyPlugin, _economyPlugin);
        _eventSubscriber.SubscribeServices(_economyPlugin.GetType().Assembly, _economyPlugin);
            
        return Task.CompletedTask;
    }
}