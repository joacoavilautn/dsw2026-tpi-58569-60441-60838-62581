using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(200);
            builder.Property(x => x.RowVersion).IsRowVersion();
        }
    }
}
