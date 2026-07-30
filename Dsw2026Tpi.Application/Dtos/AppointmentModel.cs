using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos
{
    public record AppointmentModel
    {
        public record PatientDto(int Dni);
        public record Request(Guid DoctorId, Guid AvailabilityId, PatientDto Patient, string Reason);
        public record Response(Guid Id, Guid DoctorId, Guid AvailabilityId, string Status);
    }
}
