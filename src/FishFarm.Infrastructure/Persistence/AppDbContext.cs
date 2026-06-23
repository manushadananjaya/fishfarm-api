using FishFarm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FishFarm.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Domain.Entities.FishFarm> FishFarms   => Set<Domain.Entities.FishFarm>();
    public DbSet<Person>                   People      => Set<Person>();
    public DbSet<FarmWorker>               FarmWorkers => Set<FarmWorker>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
