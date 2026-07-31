using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Dsw2026Tpi.Application.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly Dsw2026TpiDbContext _context;
    private readonly ILogger<AvailabilityService> _logger;
    public AvailabilityService(Dsw2026TpiDbContext context, ILogger<AvailabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveAvailabilityAsync(AvailabilityModel.Request request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Iniciando procesamiento de disponibilidad para el medico {request.DoctorId}.");

        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == request.DoctorId && !d.Deleted, cancellationToken);
        if (!doctorExists)
        {
            _logger.LogWarning($"Intento de asignar disponibilidad a un médico inexistente: {request.DoctorId}");
            throw new EntityNotFoundException("El medico especificado no existe.");
        } 

        var now = DateTime.Now;
        var currentYear = now.Year;
        var currentMonth = now.Month;
        var daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth);

        var parsedDays = ParseAndValidateDays(request.Days);

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            //Soft delete de los slots para limpiar
            var existingSlots = await _context.AvailabilitySlots
                .Where(s => s.DoctorId == request.DoctorId
                         && s.SlotDate.Month == currentMonth
                         && s.SlotDate.Year == currentYear
                         && s.Status == SlotStatus.AVAILABLE
                         && !s.Deleted)
                .ToListAsync(cancellationToken);
            foreach (var slot in existingSlots)
            {
                slot.SoftDelete();
            }

            //Generar las reglas y los slots de 30 minutos día a día
            for (int day = now.Day; day <= daysInMonth; day++)
            {
                var currentDate = new DateTime(currentYear, currentMonth, day);
                var dayOfWeek = currentDate.DayOfWeek;

                //Si el médico atiende este día de la semana
                if (parsedDays.Any(d => d.DayOfWeek == dayOfWeek))
                {
                    var matchingConfig = parsedDays.First(d => d.DayOfWeek == dayOfWeek);
                    var rule = new AvailabilityRule
                    {
                        DoctorId = request.DoctorId,
                        Year = currentYear,
                        Month = currentMonth,
                        DayOfWeek = dayOfWeek,
                        StartTime = matchingConfig.StartTime,
                        EndTime = matchingConfig.EndTime
                    };
                    _context.AvailabilityRules.Add(rule);

                    //Generar bloques de 30 minutos entre StartTime y EndTime
                    var currentInterval = matchingConfig.StartTime;
                    var intervalSpan = TimeSpan.FromMinutes(30);

                    while (currentInterval + intervalSpan <= matchingConfig.EndTime)
                    {
                        var slotEndTime = currentInterval + intervalSpan;
                        var slot = new AvailabilitySlot
                        {
                            DoctorId = request.DoctorId,
                            AvailabilityRule = rule,
                            SlotDate = currentDate.Date,
                            StartTime = currentInterval,
                            EndTime = slotEndTime
                        };
                        _context.AvailabilitySlots.Add(slot);
                        currentInterval = slotEndTime; // Avanzar 30 min
                    }
                }
            }
            _logger.LogInformation($"Se generaron los slots para el médico {request.DoctorId}. Guardando en BD...");

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation($"Disponibilidad guardada correctamente para el médico {request.DoctorId}.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

    }

    private List<(DayOfWeek DayOfWeek, TimeSpan StartTime, TimeSpan EndTime)> ParseAndValidateDays(List<AvailabilityModel.DayAvailability> days)
    {
        var result = new List<(DayOfWeek, TimeSpan, TimeSpan)>();
        foreach(var dayDto in days)
        {
            if (!TimeSpan.TryParse(dayDto.StartTime, out var startTime) || !TimeSpan.TryParse(dayDto.EndTime, out var endTime))
                throw new ValidationException("El formato de las horas debe ser HH:mm.", "INVALID_TIME_FORMAT");
            
            if (startTime >= endTime)
                throw new BusinessRuleException($"El horario de inicio ({dayDto.StartTime}) debe ser menor al de fin ({dayDto.EndTime}).", "INVALID_TIME_RANGE");

            var dayOfWeek = ParseDayOfWeek(dayDto.Day);

            // Validar solapamiento interno en el mismo payload
            if (result.Any(r => r.Item1 == dayOfWeek))
                throw new BusinessRuleException($"Dia {dayDto.Day} duplicado en la configuracion.", "DUPLICATE_DAY_CONFIG");
            result.Add((dayOfWeek, startTime, endTime));
        }
        return result;
    }

    private DayOfWeek ParseDayOfWeek(string day)
    {
        return day.ToUpper().Trim() switch
        {
            "LUNES" or "MONDAY" or "1" => DayOfWeek.Monday,
            "MARTES" or "TUESDAY" or "2" => DayOfWeek.Tuesday,
            "MIÉRCOLES" or "MIERCOLES" or "WEDNESDAY" or "3" => DayOfWeek.Wednesday,
            "JUEVES" or "THURSDAY" or "4" => DayOfWeek.Thursday,
            "VIERNES" or "FRIDAY" or "5" => DayOfWeek.Friday,
            "SÁBADO" or "SABADO" or "SATURDAY" or "6" => DayOfWeek.Saturday,
            "DOMINGO" or "SUNDAY" or "0" => DayOfWeek.Sunday,
            _ => throw new ValidationException($"Día no reconocido: {day}", "INVALID_DAY_NAME")
        };


    }
    public async Task<List<AvailabilityModel.DayAvailability>> GetDoctorAvailabilitiesAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        // 1. Validar que el médico exista y no esté eliminado
        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == doctorId && !d.Deleted, cancellationToken);
        if (!doctorExists)
        {
            throw new EntityNotFoundException($"Médico con ID {doctorId} no encontrado.");
        }

        // 2. Obtener las reglas de disponibilidad del médico para el mes y año actual
        var now = DateTime.Now;
        var rules = await _context.AvailabilityRules
            .Where(r => r.DoctorId == doctorId
                     && r.Year == now.Year
                     && r.Month == now.Month
                     && !r.Deleted)
            .ToListAsync(cancellationToken);

        // 3. Si no tiene disponibilidad configurada, retorna vacíos []
        if (!rules.Any())
        {
            return new List<AvailabilityModel.DayAvailability>();
        }

        // 4. Mapear las reglas a los DTOs de respuesta (Día en español y formato HH:mm)
        return rules
            .DistinctBy(r => r.DayOfWeek)
            .Select(r => new AvailabilityModel.DayAvailability(
                GetSpanishDayName(r.DayOfWeek),
                $"{r.StartTime.Hours:D2}:{r.StartTime.Minutes:D2}",
                $"{r.EndTime.Hours:D2}:{r.EndTime.Minutes:D2}"
            )).ToList();
    }

    // Método auxiliar privado para traducir el día de la semana
    private static string GetSpanishDayName(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "LUNES",
        DayOfWeek.Tuesday => "MARTES",
        DayOfWeek.Wednesday => "MIÉRCOLES",
        DayOfWeek.Thursday => "JUEVES",
        DayOfWeek.Friday => "VIERNES",
        DayOfWeek.Saturday => "SÁBADO",
        DayOfWeek.Sunday => "DOMINGO",
        _ => dayOfWeek.ToString().ToUpper()
    };

}

