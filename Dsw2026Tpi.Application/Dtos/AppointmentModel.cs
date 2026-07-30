using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos
{
    public record AppointmentModel
    {
        public record PatientDto(
            [Required(ErrorMessage = "El DNI del paciente es obligatorio.")]
            [RegularExpression(@"^\d{7,10}$", ErrorMessage = "El DNI debe tener entre 7 y 10 digitos.")]
            string Dni
            );
        public record Request(
            [Required(ErrorMessage = "El ID del doctor es obligatorio.")]
            Guid DoctorId,

            [Required(ErrorMessage = "El ID del slot de disponibilidad es obligatorio.")]
            Guid AvailabilityId,

            [Required(ErrorMessage = "El información del paciente es obligatoria.")]
            PatientDto Patient,

            [Required(ErrorMessage = "El motivo de la consulta es obligatoria.")]
            [StringLength(200, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 200 caracteres.")]
            string Reason);
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
