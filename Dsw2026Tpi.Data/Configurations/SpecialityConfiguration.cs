using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class SpecialityConfiguration : IEntityTypeConfiguration<Speciality>
{
    public void Configure(EntityTypeBuilder<Speciality> builder)
    {
        builder.ToTable("SPECIALTIES");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(s => s.Description)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(s => s.Deleted)
               .HasDefaultValue(false);

        
        builder.HasQueryFilter(s => !s.Deleted);
    }
}

