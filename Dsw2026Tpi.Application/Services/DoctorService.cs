using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;
    private readonly ILogger<DoctorService> _logger;

    public DoctorService(IPersistence persistence, ILogger<DoctorService> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        // Validar filtro de nombre si es proporcionado
        if (!string.IsNullOrWhiteSpace(name) && (name.Trim().Length < 3 || name.Trim().Length > 100))
        {
            throw new ValidationException("El filtro por nombre debe tener entre 3 y 100 caracteres.", "INVALID_NAME_FILTER");
        }

        var doctors = await _persistence.Paginate<Doctor, string>(
            pageSize, 
            pageIndex, 
            d => !d.Deleted && (string.IsNullOrWhiteSpace(name) || d.Name.Contains(name)), 
            x => x.Name, 
            nameof(Doctor.Speciality)
        );

        if (!string.IsNullOrWhiteSpace(name) && !doctors.Data.Any())
        {
            throw new EntityNotFoundException($"Médico con el nombre '{name}'");
        }

        return doctors.Map(d => new DoctorModel.Response(
            d.Id, 
            d.Name, 
            d.LicenseNumber,
            new DoctorModel.SpecialityDto(d.Speciality?.Id, d.Speciality?.Name)
        ));
    }

    public async Task<DoctorModel.Response> GetById(Guid id)
    {
        var doctor = await _persistence.First<Doctor>(d => d.Id == id && !d.Deleted, nameof(Doctor.Speciality));
        if (doctor == null)
        {
            throw new EntityNotFoundException($"Médico con ID {id} no encontrado.");
        }

        return new DoctorModel.Response(
            doctor.Id, 
            doctor.Name, 
            doctor.LicenseNumber, 
            new DoctorModel.SpecialityDto(doctor.Speciality?.Id, doctor.Speciality?.Name)
        );
    }

    public async Task<DoctorModel.Response> Create(DoctorModel.Request request)
    {
        ValidateRequest(request);

        // Validar que no exista un médico activo registrado con la misma matrícula
        var existingDoctor = await _persistence.First<Doctor>(d => d.LicenseNumber == request.LicenseNumber && !d.Deleted);
        if (existingDoctor != null)
        {
            throw new ConflictException("DOCTOR_ALREADY_EXISTS", $"Ya existe un médico activo registrado con la matrícula '{request.LicenseNumber}'.");
        }

        var speciality = await _persistence.First<Speciality>(s => s.Id == request.SpecialityId && !s.Deleted);
        if (speciality == null)
        {
            throw new EntityNotFoundException($"Especialidad con ID {request.SpecialityId} no encontrada.");
        }

        var doctor = new Doctor(request.Name, request.LicenseNumber, request.SpecialityId);
        await _persistence.Add(doctor);
        _logger.LogInformation("Médico registrado exitosamente con ID: {DoctorId}, Nombre: {DoctorName}", doctor.Id, doctor.Name);

        return new DoctorModel.Response(
            doctor.Id, 
            doctor.Name, 
            doctor.LicenseNumber, 
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name)
        );
    }

    public async Task<DoctorModel.Response> Update(Guid id, DoctorModel.Request request)
    {
        ValidateRequest(request);

        var doctor = await _persistence.First<Doctor>(d => d.Id == id && !d.Deleted);
        if (doctor == null)
        {
            throw new EntityNotFoundException($"Médico con ID {id} no encontrado.");
        }

        // Validar que no exista otro médico activo registrado con la misma matrícula
        var existingDoctor = await _persistence.First<Doctor>(d => d.LicenseNumber == request.LicenseNumber && d.Id != id && !d.Deleted);
        if (existingDoctor != null)
        {
            throw new ConflictException("DOCTOR_ALREADY_EXISTS", $"Ya existe un médico activo registrado con la matrícula '{request.LicenseNumber}'.");
        }

        var speciality = await _persistence.First<Speciality>(s => s.Id == request.SpecialityId && !s.Deleted);
        if (speciality == null)
        {
            throw new EntityNotFoundException($"Especialidad con ID {request.SpecialityId} no encontrada.");
        }

        doctor.Update(request.Name, request.LicenseNumber, request.SpecialityId);
        await _persistence.Update(doctor);
        _logger.LogInformation("Médico con ID: {DoctorId} actualizado exitosamente", doctor.Id);

        return new DoctorModel.Response(
            doctor.Id, 
            doctor.Name, 
            doctor.LicenseNumber, 
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name)
        );
    }

    private static void ValidateRequest(DoctorModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 3 || request.Name.Trim().Length > 100)
        {
            throw new ValidationException("El nombre del médico es obligatorio y debe tener entre 3 y 100 caracteres.", "INVALID_DOCTOR_NAME");
        }

        if (string.IsNullOrWhiteSpace(request.LicenseNumber))
        {
            throw new ValidationException("La matrícula del médico es obligatoria.", "INVALID_LICENSE_NUMBER");
        }

        if (request.SpecialityId == Guid.Empty)
        {
            throw new ValidationException("La especialidad es obligatoria.", "INVALID_SPECIALITY_ID");
        }
    }

    public async Task Delete(Guid id)
    {
        var doctor = await _persistence.First<Doctor>(d => d.Id == id && !d.Deleted);
        if (doctor == null)
        {
            throw new EntityNotFoundException($"Médico con ID {id} no encontrado.");
        }

        doctor.Delete();
        await _persistence.Update(doctor);
        _logger.LogInformation("Médico con ID: {DoctorId} eliminado lógicamente", id);
    }
}
