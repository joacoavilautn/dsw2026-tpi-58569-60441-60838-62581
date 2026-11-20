using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/doctors")]
[Authorize]

public class DoctorController : AppController
{
    private readonly IDoctorService _service;
    private readonly IAvailabilityService _availabilityService;
    public DoctorController(IDoctorService service, IAvailabilityService availabilityService)
    {
        _service = service;
        _availabilityService = availabilityService;
    }

    /// Obtener listado paginado de médicos activos con su especialidad (filtro opcional por nombre y especialidad).

    [HttpGet]
    [Authorize(Policy = Policies.AdminOrPatientPoliciy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0, [FromQuery] string? name = null, [FromQuery] Guid? specialtyId = null)
    {
        var result = await _service.GetAll(pageSize, pageIndex, name, specialtyId);
        return Ok(result);
    }

    /// Obtener cantidad total de doctores activos para la tarjeta del tablero.
    [HttpGet("count")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCount()
    {
        var result = await _service.GetCount();
        return Ok(result);
    }

    /// Obtener listado liviano de doctores activos para selects/dropdowns (filtrable por specialtyId).
    [HttpGet("options")]
    [Authorize(Policy = Policies.AdminOrPatientPoliciy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOptions([FromQuery] Guid? specialtyId = null)
    {
        var result = await _service.GetOptions(specialtyId);
        return Ok(result);
    }

    /// Obtener los detalles de un médico por su ID.

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AdminOrPatientPoliciy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetById(id);
        return Ok(result);
    }

    /// Registrar un nuevo médico asociado a una especialidad existente.
    
    [HttpPost]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] DoctorModel.Request request)
    {
        var result = await _service.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// Actualizar los datos de un médico existente (nombre, matrícula o especialidad).
    
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] DoctorModel.Request request)
    {
        var result = await _service.Update(id, request);
        return Ok(result);
    }

    
    /// Eliminar lógicamente a un médico por su ID (Deleted = true).
    
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.Delete(id);
        return Ok("ok");
    }

    /// Obtener la disponibilidad horaria mensual de un médico por su ID.
    [HttpGet("{id:guid}/availabilities")]
    [Authorize(Policy = Policies.AdminOrPatientPoliciy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailabilities(Guid id)
    {
        var result = await _availabilityService.GetDoctorAvailabilitiesAsync(id);
        return Ok(result);
    }

    /// Obtener los slots de 30 minutos disponibles de un médico para reserva de pacientes.
    [HttpGet("{id:guid}/slots")]
    [Authorize(Policy = Policies.AdminOrPatientPoliciy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableSlots(Guid id, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var result = await _availabilityService.GetDoctorAvailableSlotsAsync(id, from, to);
        return Ok(result);
    }


}
