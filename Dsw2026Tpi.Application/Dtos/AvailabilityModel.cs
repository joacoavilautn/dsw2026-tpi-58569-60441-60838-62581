using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos;

public record AvailabilityModel
{
    public record DayAvailability(string Day, string StartTime, string EndTime);
    public record Request(Guid DoctorId, List<DayAvailability> Days);
    public record DayAvailabilityResponse(Guid Id, string Day, string StartTime, string EndTime);
    public record Response(Guid DoctorId, List<DayAvailabilityResponse> Days);
}
