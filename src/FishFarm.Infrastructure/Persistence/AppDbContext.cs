using FishFarm.Domain.Entities;
using FishFarm.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FishFarm.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Domain.Entities.FishFarm> FishFarms => Set<Domain.Entities.FishFarm>();
    public DbSet<Worker> Workers => Set<Worker>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
