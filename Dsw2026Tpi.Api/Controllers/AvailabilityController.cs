using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[ApiController]
[Route("api/availabilities")]
[Authorize(Policy = Policies.AdminPolicy)]
public class AvailabilityController : AppController
{
    private readonly IAvailabilityService _availabilityService;
    private readonly ILogger<AvailabilityController> _logger;

    public AvailabilityController(IAvailabilityService availabilityService, ILogger<AvailabilityController> logger)
    {
        _availabilityService = availabilityService;
        _logger = logger;
    }

    //Configura la disponibilidad horaria mensual de un medico y genera sus slots de 30 minutos.
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAvailability([FromBody] AvailabilityModel.Request request)
    {
        _logger.LogInformation($"HTTP POST api/availabilities recibido para el medico {request.DoctorId}");

        var response = await _availabilityService.SaveAvailabilityAsync(request);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    //Actualiza y sobreescribe la disponibilidad horaria del mes para un medico
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAvailability([FromBody] AvailabilityModel.Request request)
    {
        _logger.LogInformation($"HTTP PUT api/availabilities recibido para el medico {request.DoctorId}");

        var response = await _availabilityService.SaveAvailabilityAsync(request);

        return Ok(response);
    }
}
