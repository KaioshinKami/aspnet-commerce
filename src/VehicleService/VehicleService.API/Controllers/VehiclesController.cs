using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleService.Application.DTOs;
using VehicleService.Application.Services;

namespace VehicleService.API.Controllers;

/// <summary>Manages the vehicle catalog — CRUD and advanced filtering.</summary>
[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly VehicleCatalogService _service;
    public VehiclesController(VehicleCatalogService service) => _service = service;

    /// <summary>Get all vehicles with optional filters.</summary>
    /// <param name="brand">Filter by brand name (partial match).</param>
    /// <param name="transmission">Automatic or Manual.</param>
    /// <param name="fuelType">Petrol, Diesel, Electric, Hybrid.</param>
    /// <param name="minPrice">Minimum price per day.</param>
    /// <param name="maxPrice">Maximum price per day.</param>
    /// <param name="minSeats">Minimum number of seats.</param>
    /// <param name="isAvailable">Filter by availability.</param>
    /// <param name="year">Filter by manufacture year.</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<VehicleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? brand,
        [FromQuery] string? transmission,
        [FromQuery] string? fuelType,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? minSeats,
        [FromQuery] bool? isAvailable,
        [FromQuery] int? year)
    {
        var filter = new VehicleFilterRequest(brand, transmission, fuelType, minPrice, maxPrice, minSeats, isAvailable, year);
        var result = await _service.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>Get a vehicle by ID.</summary>
    /// <param name="id">Vehicle GUID.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new vehicle. Requires Admin role.</summary>
    /// <param name="req">Vehicle details.</param>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateVehicleRequest req)
    {
        var result = await _service.CreateAsync(req);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Update an existing vehicle. Requires Admin role.</summary>
    /// <param name="id">Vehicle GUID.</param>
    /// <param name="req">Updated vehicle details.</param>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleRequest req)
    {
        var result = await _service.UpdateAsync(id, req);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Delete a vehicle. Requires Admin role.</summary>
    /// <param name="id">Vehicle GUID.</param>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
