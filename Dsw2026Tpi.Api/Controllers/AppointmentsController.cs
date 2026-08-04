using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.Pkcs;
using System.Text.RegularExpressions;

namespace Dsw2026Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    [Authorize]
    public class AppointmentsController : AppController
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        //Solicitar/Reservar turno medico disponible.
        [HttpPost]
        [Authorize(Policy = Policies.PatientPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> BookAppointment([FromBody] AppointmentModel.Request request)
        {        
            var response = await _appointmentService.BookAppointmentAsync(request);
            return Ok(response);
        }

        //Cancelar turno reservado
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Policies.PatientPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelAppointment(Guid id)
        {
            await _appointmentService.CancelAppointmentAsync(id);
            return Ok(new { message = "ok"});
        }

        //Ver turnos activos del paciente (solo aquellos en estado BOOKED)
        [HttpGet("patient")]
        [Authorize(Policy = Policies.AdminOrPatientPoliciy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetActiveByPatient([FromQuery] string dni, CancellationToken cancellationToken)
        {      
            var appointments = await _appointmentService.GetActiveAppointmentsByPatientDniAsync(dni);
            return Ok(appointments);
        }

        //RF09 - Busqueda avanzada y auditoria de turnos paginada
        [HttpGet("search")]
        [Authorize(Policy = Policies.AdminPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Search([FromQuery] AppointmentModel.SearchRequest search)
        {     
            var pagedResult = await _appointmentService.SearchAppointmentsAsync(search);
            return Ok(pagedResult);
        }

        //Consultar todos los turnos de un dia especifico
        [HttpGet]
        [Authorize(Policy = Policies.AdminPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByDate(
            [FromQuery] DateTime date, 
            [FromQuery] int pageSize = 10,
            [FromQuery] int pageIndex = 0)
        {        
            var result = await _appointmentService.GetAppointmentsByDateAsync(date, pageSize, pageIndex);
            return Ok(result);
        }
    }
}
