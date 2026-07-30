using Microsoft.AspNetCore.Mvc;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Services;
using System.Security.Cryptography.Pkcs;
using Microsoft.AspNetCore.Authorization;

namespace Dsw2026Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        //Solicitar/Reservar turno medico disponible.
        [HttpPost]
        [Authorize(Roles = "PACIENTE")]
        public async Task<IActionResult> BookAppointment([FromBody] AppointmentModel.Request request, CancellationToken cancellationToken)
        {
            var response = await _appointmentService.BookAppointmentAsync(request, cancellationToken);
            return Ok(response);
        }

        //Cancelar turno reservado
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "PACIENTE")]
        public async Task<IActionResult> CancelAppointment(Guid id, CancellationToken cancellationToken)
        {
            await _appointmentService.CancelAppointmentAsync(id, cancellationToken);
            return NoContent();
        }

        //Ver turnos activos del paciente (solo aquellos en estado BOOKED)
        [HttpGet("patient")]
        [Authorize(Roles = "PACIENTE, ADMINISTRADOR")]
        public async Task<IActionResult> GetActiveByPatient([FromQuery] string dni, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dni)) return BadRequest(new { Message = "El dni es obligatorio para realizar la busqueda de turnos." });

            var appointments = await _appointmentService.GetActiveAppointmentsByPatientDniAsync(dni, cancellationToken);
            return Ok(appointments);
        }
    }
}
