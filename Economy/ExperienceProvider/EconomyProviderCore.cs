using ExperienceProvider.Models;
using Microsoft.Extensions.Configuration;
using OpenMod.API;
using OpenMod.API.Eventing;
using OpenMod.API.Persistence;
using OpenMod.API.Users;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExperienceProvider
{
    public abstract class EconomyProviderCore
    {
        protected IConfiguration configuration;
        protected IDataStore dataStore;
        protected IUserManager userManager;
        protected IEventBus eventBus;
        protected IOpenModComponent openModComponent;
        protected List<Account> data;

        protected abstract Task StartAsync();

        public Task LoadAsync(IConfiguration configuration, IDataStore dataStore, IUserManager userManager, IEventBus eventBus, IOpenModComponent openModComponent)
        {
            this.configuration = configuration;
            this.dataStore = dataStore;
            this.userManager = userManager;
            this.eventBus = eventBus;
            this.openModComponent = openModComponent;

            return StartAsync();
        }
    }
}
