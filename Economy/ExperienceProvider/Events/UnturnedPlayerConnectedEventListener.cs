using Cysharp.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.Core.Users;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Players.Connections.Events;
using System.Threading.Tasks;

namespace ExperienceProvider.Events
{
    public class UnturnedPlayerConnectedEventListener : IEventListener<UnturnedPlayerConnectedEvent>
    {
        private readonly IEconomyProvider economyProvider;
        public UnturnedPlayerConnectedEventListener(IEconomyProvider economyProvider)
        {
            this.economyProvider = economyProvider;
        }
        
        public async Task HandleEventAsync(object sender, UnturnedPlayerConnectedEvent @event)
        {
            var balance = await economyProvider.GetBalanceAsync(@event.Player.SteamId.ToString(), KnownActorTypes.Player);

            if (balance == @event.Player.Player.skills.experience)
                return;

            await UniTask.SwitchToMainThread();
            @event.Player.Player.skills.ServerSetExperience((uint)balance);
            await UniTask.SwitchToThreadPool();
        }
    }
}
