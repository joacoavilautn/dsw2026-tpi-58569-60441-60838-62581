using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos
{
    public record AppointmentModel
    {
        public record PatientDto( string Dni);
        public record Request(
            Guid DoctorId,
            Guid AvailabilityId,
            PatientDto Patient,
            string Reason
            );
        public record Response(
            Guid Id, 
            Guid DoctorId, 
            Guid AvailabilityId, 
            Guid PatientId, 
            string Status, 
            string Reason, 
            DateTime CreatedAt
            );

        /*
        public record Response(
            Guid Id,
            string DoctorFullName,
            string SpecialityName,
            AvailabilityTimeDto AvailableTime,
            string PatientDni,
            string Status,
            string Reason,
            DateTime CreatedAt
        );

        public record AvailabilityTimeDto(
            DateTime Date,
            TimeSpan StartTime,
            TimeSpan EndTime
        );
         */

    }
}
