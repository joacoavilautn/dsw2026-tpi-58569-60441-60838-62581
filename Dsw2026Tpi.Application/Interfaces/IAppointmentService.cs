using Dsw2026Tpi.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.CrossCutting.Models;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentModel.Response> BookAppointmentAsync(AppointmentModel.Request request);
        Task CancelAppointmentAsync(Guid id);
        Task<IEnumerable<AppointmentModel.Response>> GetActiveAppointmentsByPatientDniAsync(string dni);
        Task<AppointmentModel.PagedResponse<AppointmentModel.Response>> SearchAppointmentsAsync(AppointmentModel.SearchRequest search);
        Task<AppointmentModel.PagedResponse<AppointmentModel.Response>> GetAppointmentsByDateAsync(DateTime date, int pageSize, int pageIndex);
    }
}
