using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishFarm.Infrastructure.Persistence.Configurations;

public sealed class FishFarmConfiguration : IEntityTypeConfiguration<Domain.Entities.FishFarm>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.FishFarm> builder)
    {
        builder.ToTable("FishFarms");

        builder.HasKey(f => f.Id);

        // FarmNumber is a non-PK IDENTITY column — SQL Server generates it automatically.
        // ValueGeneratedOnAdd()  → EF Core reads the generated value back after INSERT.
        // SetAfterSaveBehavior(Ignore) → EF Core NEVER includes this column in UPDATE
        //   statements. Without this, non-PK identity columns are still included in UPDATEs
        //   by default, which causes SQL Server error 8102 "Cannot update identity column".
        builder.Property(f => f.FarmNumber)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn(seed: 1, increment: 1)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(f => f.FarmNumber)
            .IsUnique()
            .HasDatabaseName("UIX_FishFarms_FarmNumber");

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        // GPS: 4 decimal places, e.g. 60.3913 — decimal(10,4) gives plenty of range
        builder.Property(f => f.GpsLatitude)
            .HasColumnType("decimal(10,4)")
            .IsRequired();

        builder.Property(f => f.GpsLongitude)
            .HasColumnType("decimal(10,4)")
            .IsRequired();

        builder.Property(f => f.NumberOfCages)
            .IsRequired();

        builder.Property(f => f.HasBarge)
            .IsRequired();

        builder.Property(f => f.PictureUrl)
            .HasMaxLength(1000);

        builder.Property(f => f.PicturePublicId)
            .HasMaxLength(500);

        // ── Audit fields ─────────────────────────────────────────────────────
        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.CreatedBy).HasMaxLength(256);
        builder.Property(f => f.UpdatedAt).IsRequired();
        builder.Property(f => f.UpdatedBy).HasMaxLength(256);

        // ── Soft delete ───────────────────────────────────────────────────────
        builder.Property(f => f.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(f => f.DeletedAt);
        builder.Property(f => f.DeletedBy).HasMaxLength(256);

        // Global query filter: all queries exclude soft-deleted farms by default
        builder.HasQueryFilter(f => !f.IsDeleted);

        // ── Navigation ────────────────────────────────────────────────────────
        builder.HasMany(f => f.Workers)
            .WithOne(w => w.FishFarm)
            .HasForeignKey(w => w.FishFarmId)
            .OnDelete(DeleteBehavior.Restrict); // Soft-delete cascade is handled in application layer
    }
}
