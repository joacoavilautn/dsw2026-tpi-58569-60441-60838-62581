using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class SpecialityService : ISpecialityService
{
    private readonly IPersistence _persistence;

    public SpecialityService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<SpecialityModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        // Validar filtro de nombre si es proporcionado
        if (!string.IsNullOrWhiteSpace(name) && (name.Trim().Length < 3 || name.Trim().Length > 100))
        {
            throw new ValidationException("El filtro por nombre debe tener entre 3 y 100 caracteres.", "INVALID_NAME_FILTER");
        }

        var result = await _persistence.Paginate<Speciality, string>(
            pageSize, 
            pageIndex, 
            s => !s.Deleted && (string.IsNullOrWhiteSpace(name) || s.Name.Contains(name)), 
            s => s.Name
        );

        return result.Map(s => new SpecialityModel.Response(s.Id, s.Name, s.Description));
    }

    public async Task<SpecialityModel.Response> GetById(Guid id)
    {
        var speciality = await _persistence.First<Speciality>(s => s.Id == id && !s.Deleted);
        if (speciality == null)
        {
            throw new EntityNotFoundException($"Especialidad con ID {id} no encontrada.");
        }

        return new SpecialityModel.Response(speciality.Id, speciality.Name, speciality.Description);
    }

    public async Task<SpecialityModel.Response> Create(SpecialityModel.Request request)
    {
        ValidateRequest(request);

        var existingSpeciality = await _persistence.First<Speciality>(s => s.Name == request.Name && !s.Deleted);
        if (existingSpeciality != null)
        {
            throw new ConflictException("SPECIALITY_ALREADY_EXISTS", $"Ya existe una especialidad activa registrada con el nombre '{request.Name}'.");
        }

        var speciality = new Speciality(request.Name, request.Description);
        await _persistence.Add(speciality);

        return new SpecialityModel.Response(speciality.Id, speciality.Name, speciality.Description);
    }

    public async Task<SpecialityModel.Response> Update(Guid id, SpecialityModel.Request request)
    {
        ValidateRequest(request);

        var existingSpeciality = await _persistence.First<Speciality>(s => s.Name == request.Name && !s.Deleted);
        if (existingSpeciality != null)
        {
            throw new ConflictException("SPECIALITY_ALREADY_EXISTS", $"Ya existe una especialidad activa registrada con el nombre '{request.Name}'.");
        }

        var speciality = await _persistence.First<Speciality>(s => s.Id == id && !s.Deleted);
        if (speciality == null)
        {
            throw new EntityNotFoundException($"Especialidad con ID {id} no encontrada.");
        }

        speciality.Update(request.Name, request.Description);
        await _persistence.Update(speciality);

        return new SpecialityModel.Response(speciality.Id, speciality.Name, speciality.Description);
    }

    private static void ValidateRequest(SpecialityModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 3 || request.Name.Trim().Length > 100)
        {
            throw new ValidationException("El nombre de la especialidad es obligatorio y debe tener entre 3 y 100 caracteres.", "INVALID_SPECIALITY_NAME");
        }

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length < 10 || request.Description.Trim().Length > 100)
        {
            throw new ValidationException("La descripción es obligatoria y debe tener entre 10 y 100 caracteres.", "INVALID_SPECIALITY_DESCRIPTION");
        }
    }

    public async Task Delete(Guid id)
    {
        var speciality = await _persistence.First<Speciality>(s => s.Id == id && !s.Deleted);
        if (speciality == null)
        {
            throw new EntityNotFoundException($"Especialidad con ID {id} no encontrada.");
        }

        speciality.Delete();
        await _persistence.Update(speciality);
    }
}
