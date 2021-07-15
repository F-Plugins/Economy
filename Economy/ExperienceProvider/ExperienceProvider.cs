using System;
using OpenMod.Unturned.Plugins;
using OpenMod.API.Plugins;
using Cysharp.Threading.Tasks;
using OpenMod.Extensions.Economy.Abstractions;
using Microsoft.Extensions.Configuration;
using OpenMod.API.Users;
using OpenMod.API.Eventing;
using OpenMod.API.Persistence;

[assembly: PluginMetadata("Feli.Economy.ExperienceProvider", DisplayName = "ExperienceProvider", Author = "Feli", Description = "Implementation of an EconomyProvider that works with experience", Website = "https://discord.gg/4FF2548")]
namespace ExperienceProvider
{
    public class ExperienceProvider : OpenModUnturnedPlugin
    {
        private readonly IConfiguration configuration;
        private readonly IDataStore dataStore;
        private readonly IUserManager userManager;
        private readonly IEventBus eventBus;
        private readonly IEconomyProvider economyProvider;

        public ExperienceProvider(IConfiguration configuration, IDataStore dataStore, IUserManager userManager, IEventBus eventBus, IEconomyProvider economyProvider, IServiceProvider serviceProvider) : base(serviceProvider) 
        {
            this.configuration = configuration;
            this.dataStore = dataStore;
            this.userManager = userManager;
            this.eventBus = eventBus;
            this.economyProvider = economyProvider;
        }

        protected override async UniTask OnLoadAsync()
        {
            if(economyProvider is EconomyProviderCore economyProviderCore)
            {
                await economyProviderCore.LoadAsync(configuration, dataStore, userManager, eventBus, this);
            } 
        }
    }
}
