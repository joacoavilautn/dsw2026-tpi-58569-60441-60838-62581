using Dsw2026Tpi.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAvailabilityService
{
    Task SaveAvailabilityAsync(AvailabilityModel.Request request, CancellationToken cancellationToken = default);
}
