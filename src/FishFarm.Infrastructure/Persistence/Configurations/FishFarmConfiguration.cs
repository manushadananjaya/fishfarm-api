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

        builder.Property(f => f.FarmNumber)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn(seed: 1, increment: 1)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(f => f.FarmNumber)
            .IsUnique()
            .HasDatabaseName("UIX_FishFarms_FarmNumber");

        builder.Property(f => f.Name).IsRequired().HasMaxLength(200);

        builder.Property(f => f.GpsLatitude)
            .HasColumnType("decimal(10,4)")
            .IsRequired();

        builder.Property(f => f.GpsLongitude)
            .HasColumnType("decimal(10,4)")
            .IsRequired();

        builder.Property(f => f.NumberOfCages).IsRequired();
        builder.Property(f => f.HasBarge).IsRequired();
        builder.Property(f => f.PictureUrl).HasMaxLength(1000);
        builder.Property(f => f.PicturePublicId).HasMaxLength(500);

        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.CreatedBy).HasMaxLength(256);
        builder.Property(f => f.UpdatedAt).IsRequired();
        builder.Property(f => f.UpdatedBy).HasMaxLength(256);
        builder.Property(f => f.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(f => f.DeletedAt);
        builder.Property(f => f.DeletedBy).HasMaxLength(256);

        builder.HasQueryFilter(f => !f.IsDeleted);

    }
}
