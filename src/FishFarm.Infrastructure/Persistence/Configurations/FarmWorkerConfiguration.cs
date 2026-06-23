using FishFarm.Domain.Entities;
using FishFarm.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishFarm.Infrastructure.Persistence.Configurations;

public sealed class FarmWorkerConfiguration : IEntityTypeConfiguration<FarmWorker>
{
    public void Configure(EntityTypeBuilder<FarmWorker> builder)
    {
        builder.ToTable("FarmWorkers");
        builder.HasKey(fw => fw.Id);

        builder.Property(fw => fw.Position)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(fw => fw.CreatedAt).IsRequired();
        builder.Property(fw => fw.CreatedBy).HasMaxLength(256);
        builder.Property(fw => fw.UpdatedAt).IsRequired();
        builder.Property(fw => fw.UpdatedBy).HasMaxLength(256);
        builder.Property(fw => fw.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(fw => fw.DeletedAt);
        builder.Property(fw => fw.DeletedBy).HasMaxLength(256);

        builder.HasQueryFilter(fw => !fw.IsDeleted);

        builder.HasOne(fw => fw.FishFarm)
            .WithMany(f => f.FarmWorkers)
            .HasForeignKey(fw => fw.FishFarmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fw => fw.Person)
            .WithMany(p => p.FarmWorkers)
            .HasForeignKey(fw => fw.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(fw => fw.FishFarmId)
            .HasDatabaseName("IX_FarmWorkers_FishFarmId");

        builder.HasIndex(fw => fw.PersonId)
            .HasDatabaseName("IX_FarmWorkers_PersonId");

        builder.HasIndex(fw => new { fw.FishFarmId, fw.PersonId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UIX_FarmWorkers_FarmPerson_Active");

        builder.HasIndex(fw => new { fw.FishFarmId, fw.Position })
            .HasDatabaseName("IX_FarmWorkers_FarmPosition");
    }
}
