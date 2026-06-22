using FishFarm.Domain.Entities;
using FishFarm.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishFarm.Infrastructure.Persistence.Configurations;

public sealed class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("Workers");

        builder.HasKey(w => w.Id);

        // WorkerNumber is a non-PK IDENTITY column — SQL Server generates it automatically.
        // SetAfterSaveBehavior(Ignore) prevents EF Core from including this column in
        // UPDATE statements (SQL Server error 8102 otherwise).
        builder.Property(w => w.WorkerNumber)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn(seed: 1, increment: 1)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(w => w.WorkerNumber)
            .IsUnique()
            .HasDatabaseName("UIX_Workers_WorkerNumber");

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(w => w.Age).IsRequired();

        builder.Property(w => w.Email)
            .IsRequired()
            .HasMaxLength(256);

        // ── Enum stored as INT ────────────────────────────────────────────────
        // HasConversion<int>() maps enum → int in the database column.
        // NEVER change the numeric values in WorkerPosition enum after migration.
        builder.Property(w => w.Position)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(w => w.CertifiedUntil).IsRequired();

        builder.Property(w => w.PictureUrl).HasMaxLength(1000);
        builder.Property(w => w.PicturePublicId).HasMaxLength(500);

        // ── Audit fields ─────────────────────────────────────────────────────
        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.CreatedBy).HasMaxLength(256);
        builder.Property(w => w.UpdatedAt).IsRequired();
        builder.Property(w => w.UpdatedBy).HasMaxLength(256);

        // ── Soft delete ───────────────────────────────────────────────────────
        builder.Property(w => w.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(w => w.DeletedAt);
        builder.Property(w => w.DeletedBy).HasMaxLength(256);

        // Global query filter: excludes soft-deleted workers from all queries
        builder.HasQueryFilter(w => !w.IsDeleted);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(w => w.FishFarmId)
            .HasDatabaseName("IX_Workers_FishFarmId");

        // Filtered unique index: same email can be re-used after soft-delete
        // because the uniqueness constraint only applies to active (non-deleted) rows.
        builder.HasIndex(w => w.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UIX_Workers_Email_Active");
    }
}
