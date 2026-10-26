using Azure.Core;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        public async Task<AppointmentModel.Response> BookAppointmentAsync(AppointmentModel.Request request)
        {
            if (request == null)
                throw new ValidationException("El cuerpo de la solicitud (body) no puede estar vacío.", "INVALID_BODY");

            if (request.DoctorId == Guid.Empty)
                throw new ValidationException("El identificador del médico (DoctorId) es obligatorio.", "DOCTOR_ID_REQUIRED");

            if (request.AvailabilitySlotId == Guid.Empty)
                throw new ValidationException("El identificador del slot de disponibilidad (AvailabilityId) es obligatorio.", "SLOT_ID_REQUIRED");

            if (request.Patient == null || string.IsNullOrWhiteSpace(request.Patient.Dni.ToString()))
                throw new ValidationException("La información del paciente y su DNI son obligatorios.", "PATIENT_DNI_REQUIRED");

            if (!Regex.IsMatch(request.Patient.Dni.ToString(), @"^\d{7,10}$"))
                throw new ValidationException("El DNI del paciente debe contener estrictamente entre 7 y 10 dígitos numéricos.", "INVALID_DNI");

            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 5 || request.Reason.Length > 200)
                throw new ValidationException("El motivo de la consulta es obligatorio y debe tener entre 5 y 200 caracteres.", "INVALID_REASON");

            _logger.LogInformation($"Iniciando reserva de un turno. Medico: {request.DoctorId}, slot: {request.AvailabilitySlotId} y paciente DNI: {request.Patient.Dni}");

            var doctorExists = await _context.Doctors
                .Include(d => d.Speciality)
                .FirstOrDefaultAsync(d => d.Id == request.DoctorId && !d.Deleted);
            if (doctorExists == null) throw new EntityNotFoundException("Doctor");

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Dni == request.Patient.Dni.ToString() && !p.Deleted);
            if(patient == null) throw new EntityNotFoundException("Patient");

            var slot = await _context.AvailabilitySlots.FirstOrDefaultAsync(s => s.Id == request.AvailabilitySlotId && !s.Deleted);
            if(slot == null) throw new EntityNotFoundException("AvailabilitySlot");

            if(slot.Status != SlotStatus.AVAILABLE) throw new ConflictException("SLOT_NOT_AVAILABLE", "El turno ya fue reservado o bloqueado");

            var slotFullDateTime = slot.SlotDate.Date.Add(slot.StartTime);
            if (slotFullDateTime < DateTime.UtcNow) throw new BusinessRuleException("No se pueden reservar turnos en fechas pasadas.", "PAST_DATE_NOT_ALLOWED");

            var appointment = new Appointment(
                request.DoctorId,
                request.AvailabilitySlotId, 
                patient.Id, 
                request.Reason
                );

            slot.Reserve();

            _context.Appointments.Add(appointment);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Turno {appointment.Id} reservado exitosamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("APPOINTMENT_CONFLICT", "El slot ya fue reservado por otro usuario");
            }


            return MapToResponse(appointment, doctorExists, patient);
        }

        // RF08 - Cancelar un turno reservado.
        public async Task CancelAppointmentAsync(Guid id)
        {
            if (id == Guid.Empty) 
                throw new ValidationException("El identificador del turno (ID) es obligatorio.", "ID_REQUIRED");   

            _logger.LogInformation($"Cancelando turno {id}");

            var appointment = await _context.Appointments.Include(a => a.AvailabilitySlot).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null) throw new EntityNotFoundException("Appointment");

            appointment.Cancel();      

            if(appointment.AvailabilitySlot != null)
                appointment.AvailabilitySlot.Release();

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Turno {id} cancelado exitosamente y slot liberado.");          
        }

        // Retorna la lista de turnos activos (BOOKED) de un paciente por DNI.
        public async Task<IEnumerable<AppointmentModel.Response>> GetActiveAppointmentsByPatientDniAsync(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni) || !Regex.IsMatch(dni, @"^\d{7,10}$")) 
                throw new ValidationException("El dni es obligatorio para realizar la busqueda de turnos.", "INVALID_DNI");
        
            _logger.LogInformation($"Consultando turnos activos para el paciente con DNI: {dni}");

            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Speciality)
                .Where(a => a.Patient.Dni == dni && a.Status == AppointmentStatus.BOOKED)
                .Select(a => MapToResponse(a, a.Doctor, a.Patient))
                .ToListAsync();
        }
        // RF09 - Busqueda Avanzada de turnos con filtros dinamicos y paginacion.
        public async Task<AppointmentModel.PagedResponse<AppointmentModel.Response>> SearchAppointmentsAsync(AppointmentModel.SearchRequest search)
        {
            if (search.PageNumber < 1)
                throw new ValidationException("El numero de pagina (PageNumber) debe ser mayor o igual a 1.", "INVALID_PAGENUMBER");

            if (search.PageSize < 1 || search.PageSize > 50)
                throw new ValidationException("El tamaño de pagina (PageSize) debe estar entre 1 y 50.", "INVALID_PAGESIZE");

            if (search.DateFrom.HasValue && search.DateTo.HasValue && search.DateFrom.Value > search.DateTo.Value)
                throw new ValidationException("La fecha de inicio (DateFrom) no puede ser mayor que la fecha de fin (DateTo)", "INVALID_DATE");

            _logger.LogInformation("Ejecutando busqueda avanzada de turnos con filtros.");

            var consulta = _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Speciality)
                .Include(a => a.Patient)
                .Include(a => a.AvailabilitySlot)
                .AsQueryable();

            if (search.DoctorId.HasValue)
            {
                consulta = consulta.Where(a => a.DoctorId == search.DoctorId.Value);
            }
            if (search.SpecialityId.HasValue)
            {
                consulta = consulta.Where(a => a.Doctor.SpecialityId == search.SpecialityId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search.Dni))
            {
                consulta = consulta.Where(a => a.Patient.Dni == search.Dni);
            }

            if (search.Date.HasValue)
            {
                consulta = consulta.Where(a => a.AvailabilitySlot.SlotDate.Date == search.Date.Value.Date);
            }

            if (search.DateFrom.HasValue)
            {
                consulta = consulta.Where(a => a.AvailabilitySlot.SlotDate >= search.DateFrom.Value.Date);
            }

            if (search.DateTo.HasValue)
            {
                consulta = consulta.Where(a => a.AvailabilitySlot.SlotDate <= search.DateTo.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(search.Status) && Enum.TryParse<AppointmentStatus>(search.Status, true, out var statusEnum))
            {
                consulta = consulta.Where(a => a.Status == statusEnum);
            }

            var totalCount = await consulta.CountAsync();

            var items = await consulta
                .OrderBy(a => a.AvailabilitySlot.SlotDate)
                .ThenBy(a => a.AvailabilitySlot.StartTime)
                .Skip((search.PageNumber - 1) * search.PageSize)
                .Take(search.PageSize)
                .Select(a => MapToResponse(a, a.Doctor, a.Patient))
                .ToListAsync();

            return new AppointmentModel.PagedResponse<AppointmentModel.Response>(
                Data: items,
                Total: totalCount,
                PageIndex: search.PageNumber,
                PageSize: search.PageSize
                );
        }

        //Obtiene la lista de turnos programados para una fecha especifica
        public async Task<AppointmentModel.PagedResponse<AppointmentModel.Response>> GetAppointmentsByDateAsync(DateTime date, int pageSize, int pageIndex)
        {
            if (date == default)
                throw new ValidationException("El parametro 'date' en la URL es obligatorio y debe tener un formato valido (ej. AAAA-MM-DD)","INVALID_DATE");

            _logger.LogInformation($"Consultando turnos para la fecha: {date.ToShortDateString()}");

            var query = _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Speciality)
                .Include(a => a.Patient)
                .Include(a => a.AvailabilitySlot)
                .Where(a => a.AvailabilitySlot.SlotDate.Date == date.Date);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(a => a.AvailabilitySlot.StartTime)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .Select(a => MapToResponse(a, a.Doctor, a.Patient))
                .ToListAsync();
            return new AppointmentModel.PagedResponse<AppointmentModel.Response>(
                Data: items,
                Total: totalCount,
                PageIndex: pageIndex,
                PageSize: pageSize
            );
        }

        private static AppointmentModel.Response MapToResponse(Appointment a, Doctor doctor, Patient patient)
        {
            return new AppointmentModel.Response(
                AppointmentsId: a.Id,
                AppointmentsStatus: a.Status.ToString(),
                Patient: new AppointmentModel.PatientResponseDto(
                    Dni: patient.Dni,
                    FullName: patient?.FullName ?? ""
                ),
                Doctor: new AppointmentModel.DoctorResponseDto(
                    DoctorId: doctor.Id,
                    Name: doctor.Name,
                    Specialty: new AppointmentModel.SpecialtyResponseDto(
                        SpecialtyId: doctor.SpecialityId,
                        Name: doctor.Speciality?.Name ?? "Sin Especialidad"
                    )
                ),
                Reason: a.Reason,
                CreatedAt: a.CreatedAt
            );
        }
    }
}
