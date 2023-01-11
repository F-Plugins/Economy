using Economy.API;
using Microsoft.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore.Configurator;

namespace Economy.MySql;

public class EconomyDbContext : OpenModDbContext<EconomyDbContext>
{
    public DbSet<Account> Accounts => Set<Account>();

    public EconomyDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public EconomyDbContext(IDbContextConfigurator configurator, IServiceProvider serviceProvider) : base(configurator, serviceProvider)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Account>().HasKey(a => new { a.OwnerId, a.OwnerType });
    }
}