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
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly Dsw2026TpiDbContext _context;
    private readonly ILogger<AvailabilityService> _logger;
    private readonly IHolidayProvider _holidayProvider;
    public AvailabilityService(Dsw2026TpiDbContext context, ILogger<AvailabilityService> logger, IHolidayProvider holidayProvider)
    {
        _holidayProvider = holidayProvider;
        _context = context;
        _logger = logger;
    }

    public async Task<AvailabilityModel.Response> SaveAvailabilityAsync(AvailabilityModel.Request request)
    {
        _logger.LogInformation($"Iniciando procesamiento de disponibilidad para el medico {request.DoctorId}.");
        var holidays = await _holidayProvider.GetHolidayAsync();
        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == request.DoctorId && !d.Deleted);

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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            //Soft delete de las reglas activas del medico
            var existingRule = await _context.AvailabilityRules
                .Where(s => s.DoctorId == request.DoctorId
                        && s.Month == currentMonth
                        && s.Year == currentYear
                        && !s.Deleted)
                .ToListAsync();
            foreach (var rules in existingRule)
            {
                rules.SoftDelete();
            }

            //Soft delete de los slots para limpiar
            var existingSlots = await _context.AvailabilitySlots
                .Where(s => s.DoctorId == request.DoctorId
                         && s.SlotDate.Month == currentMonth
                         && s.SlotDate.Year == currentYear
                         && s.Status == SlotStatus.AVAILABLE
                         && !s.Deleted)
                .ToListAsync();
            foreach (var slot in existingSlots)
            {
                slot.SoftDelete();
            }

            var createdRules = new List<AvailabilityRule>();
            foreach (var config in parsedDays)
            {
                var rule1 = new AvailabilityRule
                {
                    DoctorId = request.DoctorId,
                    Year = currentYear,
                    Month = currentMonth,
                    DayOfWeek = config.DayOfWeek,
                    StartTime = config.StartTime,
                    EndTime = config.EndTime
                };

                _context.AvailabilityRules.Add(rule1);
                createdRules.Add(rule1);
            }
   
            //Generar las reglas y los slots de 30 minutos día a día
            for (int day = now.Day; day <= daysInMonth; day++)
            {
                var currentDate = new DateTime(currentYear, currentMonth, day);
                var dayOfWeek = currentDate.DayOfWeek;

                if (holidays.Contains(DateOnly.FromDateTime(currentDate)))
                {
                    _logger.LogInformation($"Dia {currentDate} omitido por la generacion de turnos por ser feriado.");
                    continue;
                }

                var matchingRule = createdRules.FirstOrDefault(r => r.DayOfWeek == dayOfWeek);

                //Si el médico atiende este día de la semana
                if (matchingRule != null)
                {
                    //Generar bloques de 30 minutos entre StartTime y EndTime
                    var currentInterval = matchingRule.StartTime;
                    var intervalSpan = TimeSpan.FromMinutes(30);

                    while (currentInterval + intervalSpan <= matchingRule.EndTime)
                    {
                        var slotEndTime = currentInterval + intervalSpan;
                        var slot = new AvailabilitySlot
                        {
                            DoctorId = request.DoctorId,
                            AvailabilityRule = matchingRule,
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

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"Disponibilidad guardada correctamente para el médico {request.DoctorId}.");

            var responseDays = createdRules.Select(r => new AvailabilityModel.DayAvailabilityResponse(
                r.Id,
                r.DayOfWeek.ToString(),
                r.StartTime.ToString(@"hh\:mm"),
                r.EndTime.ToString(@"hh\:mm")
            )).ToList();

            return new AvailabilityModel.Response(request.DoctorId, responseDays);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

    }

    private List<(DayOfWeek DayOfWeek, TimeSpan StartTime, TimeSpan EndTime)> ParseAndValidateDays(List<AvailabilityModel.DayAvailability> days)
    {
        var result = new List<(DayOfWeek, TimeSpan, TimeSpan)>();
        foreach(var dayDto in days)
        {
            if (!TimeOnly.TryParse(dayDto.StartTime, out var parsedStart) || !TimeOnly.TryParse(dayDto.EndTime, out var parsedEnd))
                throw new ValidationException("El formato de las horas debe ser HH:mm.", "INVALID_TIME_FORMAT");
            
            if (parsedStart >= parsedEnd)
                throw new BusinessRuleException($"El horario de inicio ({dayDto.StartTime}) debe ser menor al de fin ({dayDto.EndTime}).", "INVALID_TIME_RANGE");

            var startTime = parsedStart.ToTimeSpan();
            var endTime = parsedEnd.ToTimeSpan();
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
    public async Task<List<AvailabilityModel.DayAvailabilityResponse>> GetDoctorAvailabilitiesAsync(Guid doctorId)
    {
        // 1. Validar que el médico exista y no esté eliminado
        var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == doctorId && !d.Deleted);
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
            .ToListAsync();

        // 3. Si no tiene disponibilidad configurada, retorna vacíos []
        if (!rules.Any())
        {
            return new List<AvailabilityModel.DayAvailabilityResponse>();
        }

        // 4. Mapear las reglas a los DTOs de respuesta (Id, Día en español y formato HH:mm)
        return rules
            .DistinctBy(r => r.DayOfWeek)
            .Select(r => new AvailabilityModel.DayAvailabilityResponse(
                r.Id,
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

