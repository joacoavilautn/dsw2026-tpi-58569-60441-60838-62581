namespace Dsw2026Tpi.Application.Dtos;

public record DoctorModel
{
    public record Request(string Name, string LicenseNumber, Guid SpecialtyId);
    public record Response(Guid Id, string Name, string LicenseNumber, SpecialityDto? Specialty);
    public record SpecialityDto(Guid? Id, string? Name);
    public record OptionResponse(Guid Id, string Name, string LicenseNumber, Guid? SpecialtyId, string? SpecialtyName);
    public record CountResponse(int Total, int AddedThisMonth);
}
