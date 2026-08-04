using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography.Pkcs;
using System.Text.RegularExpressions;

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
        [EnableRateLimiting("AppointmentBookingPolicy")]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [Authorize(Roles = "PACIENTE")]
        public async Task<IActionResult> BookAppointment([FromBody] AppointmentModel.Request request)
        {
            if (request == null)
            {
                return BadRequest("El cuerpo de la solicitud (body) no puede estar vacío.");
            }

            if (request.DoctorId == Guid.Empty)
            {
                return BadRequest("El identificador del médico (DoctorId) es obligatorio.");
            }

            if (request.AvailabilityId == Guid.Empty)
            {
                return BadRequest("El identificador del slot de disponibilidad (AvailabilityId) es obligatorio.");
            }

            if (request.Patient == null || string.IsNullOrWhiteSpace(request.Patient.Dni))
            {
                return BadRequest("La información del paciente y su DNI son obligatorios.");
            }

            if (!Regex.IsMatch(request.Patient.Dni, @"^\d{7,10}$"))
            {
                return BadRequest("El DNI del paciente debe contener estrictamente entre 7 y 10 dígitos numéricos.");
            }

            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 5 || request.Reason.Length > 200)
            {
                return BadRequest("El motivo de la consulta es obligatorio y debe tener entre 5 y 200 caracteres.");
            }

            var response = await _appointmentService.BookAppointmentAsync(request);
            return Ok(response);
        }

        //Cancelar turno reservado
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "PACIENTE")]
        public async Task<IActionResult> CancelAppointment(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("El identificador del turno (ID) es obligatorio.");
            }

            await _appointmentService.CancelAppointmentAsync(id);
            return NoContent();
        }

        //Ver turnos activos del paciente (solo aquellos en estado BOOKED)
        [HttpGet("patient")]
        [Authorize(Roles = "PACIENTE, ADMINISTRADOR")]
        public async Task<IActionResult> GetActiveByPatient([FromQuery] string dni, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dni)) return BadRequest(new { Message = "El dni es obligatorio para realizar la busqueda de turnos." });

            if (!Regex.IsMatch(dni, @"^\d{7,10}$")) return BadRequest("El DNI ingresado debe contener entre 7 y 10 dígitos numéricos.");
            
            var appointments = await _appointmentService.GetActiveAppointmentsByPatientDniAsync(dni);
            return Ok(appointments);
        }

        //RF09 - Busqueda avanzada y auditoria de turnos paginada
        [HttpGet("search")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> Search([FromQuery] AppointmentModel.SearchRequest search)
        {
            if (search.PageNumber < 1)
                return BadRequest("El numero de pagina (PageNumber) debe ser mayor o igual a 1.");

            if (search.PageSize < 1 || search.PageSize > 50)
                return BadRequest("El tamaño de pagina (PageSize) debe estar entre 1 y 50.");

            if (search.DateFrom.HasValue && search.DateTo.HasValue && search.DateFrom.Value > search.DateTo.Value)
                return BadRequest("La fecha de inicio (DateFrom) no puede ser mayor que la fecha de fin (DateTo)");

            var pagedResult = await _appointmentService.SearchAppointmentsAsync(search);
            return Ok(pagedResult);
        }

        //Consultar todos los turnos de un dia especifico
        [HttpGet]
        [Authorize(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> GetByDate([FromQuery] DateTime date)
        {
            if (date == default)
                return BadRequest("El parametro 'date' en la URL es obligatorio y debe tener un formato valido (ej. AAAA-MM-DD)");

            var appointments = await _appointmentService.GetAppointmentsByDateAsync(date);
            return Ok(appointments);
        }
    }
}
