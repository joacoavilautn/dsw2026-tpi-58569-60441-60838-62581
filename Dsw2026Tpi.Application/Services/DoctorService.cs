using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    public DoctorService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        var doctors = await _persistence.Paginate<Doctor, string>(
            pageSize, 
            pageIndex, 
            d => !d.Deleted && (string.IsNullOrWhiteSpace(name) || d.Name.Contains(name)), 
            x => x.Name, 
            nameof(Doctor.Speciality)
        );

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
        var speciality = await _persistence.First<Speciality>(s => s.Id == request.SpecialityId && !s.Deleted);
        if (speciality == null)
        {
            throw new EntityNotFoundException($"Especialidad con ID {request.SpecialityId} no encontrada.");
        }

        var doctor = new Doctor(request.Name, request.LicenseNumber, request.SpecialityId);
        await _persistence.Add(doctor);

        return new DoctorModel.Response(
            doctor.Id, 
            doctor.Name, 
            doctor.LicenseNumber, 
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name)
        );
    }

    public async Task<DoctorModel.Response> Update(Guid id, DoctorModel.Request request)
    {
        var doctor = await _persistence.First<Doctor>(d => d.Id == id && !d.Deleted);
        if (doctor == null)
        {
            throw new EntityNotFoundException($"Médico con ID {id} no encontrado.");
        }

        var speciality = await _persistence.First<Speciality>(s => s.Id == request.SpecialityId && !s.Deleted);
        if (speciality == null)
        {
            throw new EntityNotFoundException($"Especialidad con ID {request.SpecialityId} no encontrada.");
        }

        doctor.Update(request.Name, request.LicenseNumber, request.SpecialityId);
        await _persistence.Update(doctor);

        return new DoctorModel.Response(
            doctor.Id, 
            doctor.Name, 
            doctor.LicenseNumber, 
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name)
        );
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
    }
}
