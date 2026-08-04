using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos
{
    public record AppointmentModel
    {
        public record PatientDto(long Dni);
        public record Request(
            Guid DoctorId,
            Guid AvailabilitySlotId,
            PatientDto Patient,
            string Reason
            );
        public record PatientResponseDto(
            string Dni,
            string? FullName
            );
        public record SpecialtyResponseDto(
            Guid SpecialtyId,
            string Name
            );
        public record DoctorResponseDto(
            Guid DoctorId,
            string Name,
            SpecialtyResponseDto Specialty
            );
        public record Response(
            Guid AppointmentsId,
            string AppointmentsStatus,
            DoctorResponseDto Doctor,       
            PatientResponseDto Patient, 
            string Reason, 
            DateTime CreatedAt
            );
        public record SearchRequest(
            Guid? DoctorId,
            Guid? SpecialityId,
            string? Dni,
            DateTime? Date,
            DateTime? DateFrom,
            DateTime? DateTo,
            string? Status,
            int PageNumber = 1,
            int PageSize = 10
            );
        public record PagedResponse<T>(
            IEnumerable<T> Data,
            int Total,
            int PageIndex,
            int PageSize
            );       
    }
}
