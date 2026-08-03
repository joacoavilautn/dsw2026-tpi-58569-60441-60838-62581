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
        public record SearchRequest(
            Guid? DoctorId,
            Guid? SpecialityId,
            DateTime? DateFrom,
            DateTime? DateTo,
            string? Status,
            int PageNumber = 1,
            int PageSize = 10
            );
        public record PagedResponse<T>(
            IEnumerable<T> Items,
            int TotalCount,
            int PageNumber,
            int PageSize
            )
        {
            public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
            public bool HasNextPage => PageNumber < TotalPages;
            public bool HasPreviousPage => PageNumber > 1;
        }


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
