namespace Dsw2026Tpi.Application.Dtos;

public static class SpecialityModel
{
    public record Request(string Name, string Description);
    public record Response(Guid Id, string Name, string Description);
}
