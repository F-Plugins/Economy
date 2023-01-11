using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;
using Economy.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMod.API.Ioc;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Users;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Players.Connections.Events;
using OpenMod.Unturned.Players.Skills.Events;
using OpenMod.Unturned.Players.Stats.Events;
using OpenMod.Unturned.Users;
using OpenMod.Unturned.Users.Events;
using SDG.Unturned;
using SilK.Unturned.Extras.Events;
using Steamworks;

namespace Economy;

[PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Lowest)]
public class ExperienceSynchronizer : IExperienceSynchronizer,
    IInstanceEventListener<UnturnedUserConnectedEvent>,
    IInstanceAsyncEventListener<UnturnedPlayerExperienceUpdatedEvent>,
    IInstanceAsyncEventListener<BalanceUpdatedEvent>,
    IInstanceEventListener<UnturnedPlayerDisconnectedEvent>,
    IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<CSteamID, uint> _playerBalance; // Dictionary used to avoid calling the economy provider in the experience update method
    private readonly IEconomyProvider _economyProvider;
    private readonly IUnturnedUserDirectory _unturnedUserDirectory;

    public ExperienceSynchronizer(
        IConfiguration configuration,
        IEconomyProvider economyProvider,
        IUnturnedUserDirectory unturnedUserDirectory)
    {
        _configuration = configuration;
        _playerBalance = new ConcurrentDictionary<CSteamID, uint>();
        _economyProvider = economyProvider;
        _unturnedUserDirectory = unturnedUserDirectory;
    }

    public async UniTask HandleEventAsync(object? sender, UnturnedUserConnectedEvent @event)
    {
        if (!ShouldSync())
            return;
        
        var balance = (uint)await _economyProvider.GetBalanceAsync(@event.User.Id, @event.User.Type);
        var xp = @event.User.Player.Player.skills.experience;
        
        // if xp is not == balance we set it to the persisted balance
        if (balance != xp)
        {
            _playerBalance.TryAdd(@event.User.SteamId, balance);
            @event.User.Player.Player.skills.ServerSetExperience(balance);
        }
    }

    public async UniTask HandleEventAsync(object? sender, UnturnedPlayerExperienceUpdatedEvent @event)
    {
        if (!ShouldSync())
            return;
        
        var experience = @event.Player.Player.skills.experience;
        
        // if the old player balance is not found we get it from the db
        if (!_playerBalance.TryGetValue(@event.Player.SteamId, out var balance))
        { 
            balance = (uint)await _economyProvider.GetBalanceAsync(@event.Player.SteamId.ToString(), KnownActorTypes.Player);
        }
        
        // if the experience == balance it means that the experience is already sync
        if(experience == balance)
            return;
        
        // we update the player balance 
        _playerBalance.AddOrUpdate(@event.Player.SteamId, experience, (_, _) => experience);

        // persist the player xp as the balance
        await _economyProvider.UpdateBalanceAsync(@event.Player.SteamId.ToString(), KnownActorTypes.Player,
            (decimal)experience - balance, "experience");
    }

    public async UniTask HandleEventAsync(object? sender, BalanceUpdatedEvent @event)
    {
        if (@event.OwnerType != KnownActorTypes.Player)
            return;
        
        if (!ShouldSync())
            return;

        var user = _unturnedUserDirectory.FindUser(@event.OwnerId, UserSearchMode.FindById);
        
        if(user is null)
            return;
        
        // if the experience == balance it means that the balance is already sync with the experience
        if(user.Player.Player.skills.experience == @event.NewBalance)
            return;
        
        // update player experience
        await UniTask.SwitchToMainThread();
        var balance = (uint)@event.NewBalance;
        
        // set the player balance
        _playerBalance.AddOrUpdate(user.SteamId, balance, (_, _) => balance);

        if(balance > user.Player.Player.skills.experience)
            user.Player.Player.skills.askAward(balance - user.Player.Player.skills.experience);
        else 
            user.Player.Player.skills.askSpend(user.Player.Player.skills.experience - balance);
    }
    
    public UniTask HandleEventAsync(object? sender, UnturnedPlayerDisconnectedEvent @event)
    {
        _playerBalance.TryRemove(@event.Player.SteamId, out _);
        return UniTask.CompletedTask;
    }
    
    public void Dispose()
    {
        _playerBalance.Clear();
    }

    private bool ShouldSync()
    {
        return _configuration.GetSection("economy:use_xp").Get<bool>();
    }
}