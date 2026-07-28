using Microsoft.AspNetCore.Mvc;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Services;
using System.Security.Cryptography.Pkcs;

namespace Dsw2026Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpPost]
        public async Task<IActionResult> BookAppointment([FromBody] AppointmentModel.Request request, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.BookAppointmentAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> CancelAppointment(Guid id, CancellationToken cancellationToken)
        {
            var result = await _appointmentService.CancelAppointmentAsync(id, cancellationToken);
            return Ok(result);
        }
    }
}
