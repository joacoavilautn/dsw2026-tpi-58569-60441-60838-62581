using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Appointment : EntityBase
    {
        public Guid DoctorId { get; init; }
        public Doctor? Doctor { get; init; }
        public Guid AvailabilitySlotId { get; init; }
        public AvailabilitySlot? AvailabilitySlot { get; init; }
        public Guid PatientId { get; init; }
        public Patient? Patient { get; init; }
        public string Reason { get; private set; }
        public AppointmentStatus Status { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public DateTime? AttendedAt { get; private set; }

        public Appointment() { }
        public Appointment(Guid doctorId, Guid availabilitySlotId, Guid patientId, string reason)
        {
            if(string.IsNullOrWhiteSpace(reason)) throw new BusinessRuleException("El motivo de la cita es obligatorio", "REASON_REQUIRED");
            if(reason.Length < 5) throw new BusinessRuleException("El motivo de la cita debe tener minimo 5 caracteres", "REASON_TOO_SHORT");
            
            DoctorId = doctorId;
            AvailabilitySlotId = availabilitySlotId;
            PatientId = patientId;
            Reason = reason;
            Status = AppointmentStatus.BOOKED;
        }

        public void Cancel()
        {
            if(Status != AppointmentStatus.BOOKED)
            {
                throw new BusinessRuleException("Solo se puede cancelar un turno previamente reservado", "INVALID_STATUS");
            }
            Status = AppointmentStatus.CANCELLED;
            CancelledAt = DateTime.UtcNow;
        }
        public void Attend()
        {
            if(Status != AppointmentStatus.BOOKED)
            {
                throw new BusinessRuleException("Solo se puede atender un turno previamente reservado", "INVALID_STATUS");
            }
            Status = AppointmentStatus.ATTENDED;
            AttendedAt = DateTime.UtcNow;
        }
        public void RegisterNoShow()
        {
            if (Status != AppointmentStatus.BOOKED)
            {
                throw new BusinessRuleException("Solo se puede marcar como No Show un turno previamente reservado", "INVALID_STATUS");
            }
            Status = AppointmentStatus.NO_SHOW;
        }
    }
}
