using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Interfaces;

public interface IHolidayProvider
{
    Task<HashSet<DateOnly>> GetHolidayAsync();
}
