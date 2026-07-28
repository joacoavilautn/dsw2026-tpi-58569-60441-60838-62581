using Dsw2026Tpi.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.CrossCutting.Models;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentModel.Response> BookAppointmentAsync(AppointmentModel.Request request, CancellationToken cancellationToken = default);
        Task<bool> CancelAppointmentAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
