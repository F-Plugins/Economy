using OpenMod.API.Eventing;
using OpenMod.Core.Users;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Players.Skills.Events;
using System.Threading.Tasks;

namespace ExperienceProvider.Events
{
    public class UnturnedPlayerExperienceUpdatedEventListener : IEventListener<UnturnedPlayerExperienceUpdatedEvent>
    {
        private readonly IEconomyProvider economyProvider;
        public UnturnedPlayerExperienceUpdatedEventListener(IEconomyProvider economyProvider)
        {
            this.economyProvider = economyProvider;
        }

        public async Task HandleEventAsync(object sender, UnturnedPlayerExperienceUpdatedEvent @event)
        {
            var balance = await economyProvider.GetBalanceAsync(@event.Player.SteamId.ToString(), KnownActorTypes.Player);

            if (balance == @event.Player.Player.skills.experience)
                return;
                
            await economyProvider.SetBalanceAsync(@event.Player.SteamId.ToString(), KnownActorTypes.Player, @event.Player.Player.skills.experience);
        }
    }
}
