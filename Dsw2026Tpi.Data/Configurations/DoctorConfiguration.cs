using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("DOCTORS");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(d => d.LicenseNumber)
               .HasMaxLength(50);

        builder.Property(d => d.Deleted)
               .HasDefaultValue(false);

        // Relación 1 Especialidad a Muchos Médicos
        builder.HasOne(d => d.Speciality)
               .WithMany()
               .HasForeignKey(d => d.SpecialityId)
               .OnDelete(DeleteBehavior.Restrict);

        // Filtro global para Soft Delete (Convención TPI)
        builder.HasQueryFilter(d => !d.Deleted);
    }
}
