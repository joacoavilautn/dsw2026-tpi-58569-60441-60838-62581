using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Azure.Core;

namespace Dsw2026Tpi.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly Dsw2026TpiDbContext _context;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(Dsw2026TpiDbContext context, ILogger<AppointmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // RF07 - Solicitar / Reservar un turno medico disponible
        public async Task<AppointmentModel.Response> BookAppointmentAsync(AppointmentModel.Request request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Iniciando reserva de un turno. Medico: {request.DoctorId}, slot: {request.AvailabilityId} y paciente DNI: {request.Patient.Dni}");

            var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == request.DoctorId && !d.Deleted, cancellationToken);
            if (!doctorExists) throw new EntityNotFoundException("Doctor");

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Dni == request.Patient.Dni.ToString() && !p.Deleted, cancellationToken);
            if(patient == null) throw new EntityNotFoundException("Patient");

            var slot = await _context.AvailabilitySlots.FirstOrDefaultAsync(s => s.Id == request.AvailabilityId && !s.Deleted, cancellationToken);
            if(slot == null) throw new EntityNotFoundException("AvailabilitySlot");

            if(slot.Status != SlotStatus.AVAILABLE) throw new ConflictException("SLOT_NOT_AVAILABLE", "El turno ya fue reservado o bloqueado");

            var slotFullDateTime = slot.SlotDate.Date.Add(slot.StartTime);
            if (slotFullDateTime < DateTime.UtcNow) throw new BusinessRuleException("No se pueden reservar turnos en fechas pasadas.", "PAST_DATE_NOT_ALLOWED");

            var appointment = new Appointment(
                request.DoctorId,
                request.AvailabilityId, 
                patient.Id, 
                request.Reason
                );

            slot.Reserve();

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"Turno {appointment.Id} reservado exitosamente.");
          

            return new AppointmentModel.Response(
                appointment.Id,
                appointment.DoctorId,
                appointment.AvailabilitySlotId,
                appointment.PatientId,
                appointment.Status.ToString(),
                appointment.Reason,
                appointment.CreatedAt
                );

            /*
             var appointmentDetails = await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Speciality)
                .Include(a => a.AvailabilitySlot)
                .Include(a => a.Patient)
                .FirstAsync(a => a.Id == appointment.Id, cancellationToken);

            return new AppointmentModel.Response(
                appointmentDetails.Id,
                $"{appointmentDetails.Doctor.FirstName} {appointmentDetails.Doctor.LastName}",
                appointmentDetails.Doctor.Speciality?.Name ?? "Sin Especialidad",
                appointmentDetails.AvailabilitySlot.Date,
                appointmentDetails.AvailabilitySlot.StartTime,
                appointmentDetails.AvailabilitySlot.EndTime,
                appointmentDetails.Patient.Dni,
                appointmentDetails.Status.ToString(),
                appointmentDetails.Reason,
                appointmentDetails.CreatedAt
                );
             */
        }

        // RF08 - Cancelar un turno reservado.
        public async Task CancelAppointmentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Cancelando turno {id}");

            var appointment = await _context.Appointments.Include(a => a.AvailabilitySlot).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (appointment == null) throw new EntityNotFoundException("Appointment");

            appointment.Cancel();      

            if(appointment.AvailabilitySlot != null)
            {
                appointment.AvailabilitySlot.Release();
            }
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"Turno {id} cancelado exitosamente y slot liberado.");          
        }

        // Retorna la lista de turnos activos (BOOKED) de un paciente por DNI.
        public async Task<IEnumerable<AppointmentModel.Response>> GetActiveAppointmentsByPatientDniAsync(string dni, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Consultando turnos activos para el paciente con DNI: {dni}");

            return await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.Patient.Dni == dni && a.Status == AppointmentStatus.BOOKED)
                .Select(a => new AppointmentModel.Response(
                    a.Id,
                    a.DoctorId,
                    a.AvailabilitySlotId,
                    a.PatientId,
                    a.Status.ToString(),
                    a.Reason,
                    a.CreatedAt
                    )).ToListAsync(cancellationToken);

            /*
            return await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Speciality)
                .Include(a => a.AvailabilitySlot)
                .Include(a => a.Patient)
                .Where(a => a.Patient.Dni == dni 
                            && a.Status == AppointmentStatus.BOOKED 
                            && !a.Deleted)
                .Select(a => new AppointmentModel.Response(
                    a.Id,
                    $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                    a.Doctor.Speciality != null ? a.Doctor.Speciality.Name : "Sin Especialidad",
                    a.AvailabilitySlot.Date,
                    a.AvailabilitySlot.StartTime,
                    a.AvailabilitySlot.EndTime,
                    a.Patient.Dni,
                    a.Status.ToString(),
                    a.Reason,
                    a.CreatedAt
                )).ToListAsync(cancellationToken);
             */
        }
    }
}
