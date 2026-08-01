using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities;

public class AvailabilitySlot : EntityBase
{
    public Guid? AvaibilityRuleId { get; init; }
    public AvailabilityRule? AvailabilityRule { get; init; }
    public Guid DoctorId { get; init; }
    public Doctor Doctor { get; init; } = null!;
    public DateTime SlotDate { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }

    [ConcurrencyCheck]
    public SlotStatus Status { get; private set; } = SlotStatus.AVAILABLE;
    public bool Deleted { get; private set; } = false;

    public void Reserve()
    {
        if (Status != SlotStatus.AVAILABLE)
            throw new InvalidOperationException("El slot no esta disponible para reserva.");
        Status = SlotStatus.BOOKED;
    }
    public void Release()
    {
        Status = SlotStatus.AVAILABLE;
    }
    public void Block()
    {
        Status = SlotStatus.BLOCKED;
    }
    public void SoftDelete()
    {
        Deleted = true;
    }

}
