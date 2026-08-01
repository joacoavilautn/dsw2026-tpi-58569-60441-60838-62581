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

        public AppointmentService(Dsw2026TpiDbContext dbContext, ILogger<AppointmentService> logger)
        {
            _context = dbContext;
            _logger = logger;
        }
        public async Task<AppointmentModel.Response> BookAppointmentAsync(AppointmentModel.Request request)
        {
            _logger.LogInformation($"Iniciando reserva de un turno para el slot {request.AvailabilitySlotId} y paciente DNI {request.Patient.Dni}");

            var patient = await _context.Set<Patient>().FirstOrDefaultAsync(p => p.Dni == request.Patient.Dni.ToString() && !p.Deleted);
            if(patient == null) throw new EntityNotFoundException("Patient");

            var slot = await _context.Set<AvailabilitySlot>().FirstOrDefaultAsync(s => s.Id == request.AvailabilitySlotId && !s.Deleted);
            if(slot == null) throw new EntityNotFoundException("AvailabilitySlot");

            if(slot.Status != SlotStatus.AVAILABLE) throw new ConflictException("APPOINTMENT_CONFLICT", "El turno ya fue reservado o bloqueado");

            var slotFullDateTime = slot.SlotDate.Date.Add(slot.StartTime);
            if (slotFullDateTime < DateTime.Now) throw new BusinessRuleException("No se pueden reservar turnos en fechas pasadas.", "PAST_DATE_NOT_ALLOWED");

            var appointment = new Appointment(request.DoctorId, slot.Id, patient.Id, request.Reason);
            slot.Reserve();

            _context.Appointments.Add(appointment);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, $"Doble reserva detectada en el slot {slot.Id}");
                throw new ConflictException("APPOINTMENT_CONFLICT", "El turno ya esta reservado");
            }

            return new AppointmentModel.Response(
                appointment.Id,
                appointment.DoctorId,
                appointment.AvailabilitySlotId,
                appointment.Status.ToString()
                );
        }

        public async Task<bool> CancelAppointmentAsync(Guid id)
        {
            _logger.LogInformation($"Cancelando turno {id}");

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null) throw new EntityNotFoundException("Appointment");

            appointment.Cancel(DateTime.UtcNow);

            var slot = await _context.Set<AvailabilitySlot>().FirstOrDefaultAsync(s => s.Id == appointment.AvailabilitySlotId);

            if(slot != null)
            {
                slot.Release();
            }
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
