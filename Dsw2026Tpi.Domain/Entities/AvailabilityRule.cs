using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities;

public class AvailabilityRule : EntityBase
{
    public Guid DoctorId { get; init; }
    public Doctor Doctor { get; init; } = null!;
    public int Month { get; init; }
    public int Year { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public bool Deleted { get; private set; } = false;

    public void SoftDelete()
    {
        Deleted = true;
    }
}
