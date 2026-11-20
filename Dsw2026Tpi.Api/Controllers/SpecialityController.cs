using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/specialties")]
[Authorize]
public class SpecialityController : AppController
{
    private readonly ISpecialityService _service;

    public SpecialityController(ISpecialityService service)
    {
        _service = service;
    }


    /// Obtener listado paginado de especialidades activas (filtro opcional por nombre).

    [HttpGet]
    [Authorize(Policy = Policies.AdminOrPatientPoliciy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0, [FromQuery] string? name = null)
    {
        var result = await _service.GetAll(pageSize, pageIndex, name);
        return Ok(result);
    }

    /// Obtener cantidad total de especialidades activas para la tarjeta del tablero.
    [HttpGet("count")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCount()
    {
        var result = await _service.GetCount();
        return Ok(result);
    }

    /// Obtener listado liviano de especialidades activas para selects/dropdowns.
    [HttpGet("options")]
    [Authorize(Policy = Policies.AdminOrPatientPoliciy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOptions()
    {
        var result = await _service.GetOptions();
        return Ok(result);
    }

    /// Obtener los detalles de una especialidad por su ID.

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AdminOrPatientPoliciy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetById(id);
        return Ok(result);
    }

    
    /// Crear una nueva especialidad médica.
   
    [HttpPost]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] SpecialityModel.Request request)
    {
        var result = await _service.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    
    /// Actualizar el nombre o descripción de una especialidad existente.
    
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SpecialityModel.Request request)
    {
        var result = await _service.Update(id, request);
        return Ok(result);
    }

    
    /// Eliminar lógicamente una especialidad por su ID (Deleted = true).
    
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.Delete(id);
        return Ok("ok");
    }
}
