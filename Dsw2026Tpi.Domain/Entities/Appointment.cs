using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Appointment : EntityBase
    {
        public Guid DoctorId { get; init; }
        public Doctor Doctor { get; init; }
        public Guid AvailabilitySlotId { get; init; }
        public AvailabilitySlot AvailabilitySlot { get; init; }
        public Guid PatientId { get; init; }
        public Patient Patient { get; init; }
        public string Reason { get; private set; }
        public AppointmentStatus Status { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public DateTime? AttendedAt { get; private set; }

        public Appointment() { }
        public Appointment(Guid doctorId, Guid availabilitySlotId, Guid patientId, string reason)
        {
            DoctorId = doctorId;
            AvailabilitySlotId = availabilitySlotId;
            PatientId = patientId;
            Reason = reason;
            Status = AppointmentStatus.BOOKED;
        }

        public void Cancel(DateTime cancellationDate)
        {
            if(Status != AppointmentStatus.BOOKED)
            {
                throw new BusinessRuleException("Solo se puede cancelar un turno en estado BOOKED", "INVALID_STATUS");
            }
            Status = AppointmentStatus.CANCELLED;
            CancelledAt = cancellationDate;
        }
        public void Attend(DateTime attendanceDate)
        {
            if(Status != AppointmentStatus.BOOKED)
            {
                throw new BusinessRuleException("Solo se puede atender un turno en estado BOOKED", "INVALID_STATUS");
            }
            Status = AppointmentStatus.ATTENDED;
            AttendedAt = attendanceDate;
        }
    }
}
