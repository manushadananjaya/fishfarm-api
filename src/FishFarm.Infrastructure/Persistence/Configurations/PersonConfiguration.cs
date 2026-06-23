using FishFarm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishFarm.Infrastructure.Persistence.Configurations;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("People");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PersonNumber)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn(seed: 1, increment: 1)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(p => p.PersonNumber)
            .IsUnique()
            .HasDatabaseName("UIX_People_PersonNumber");

        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Email).IsRequired().HasMaxLength(256);
        builder.Property(p => p.Age).IsRequired();
        builder.Property(p => p.CertifiedUntil).IsRequired();
        builder.Property(p => p.PictureUrl).HasMaxLength(1000);
        builder.Property(p => p.PicturePublicId).HasMaxLength(500);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.UpdatedAt).IsRequired();
        builder.Property(p => p.UpdatedBy).HasMaxLength(256);
        builder.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAt);
        builder.Property(p => p.DeletedBy).HasMaxLength(256);

        builder.HasQueryFilter(p => !p.IsDeleted);

        // Email must be unique across active (non-deleted) persons.
        builder.HasIndex(p => p.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UIX_People_Email_Active");
    }
}
