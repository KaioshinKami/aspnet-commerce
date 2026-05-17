using System.Security.Claims;
using BookingService.Application.DTOs;
using BookingService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.API.Controllers;

/// <summary>Manages car rental bookings.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly BookingManagementService _service;
    public BookingsController(BookingManagementService service) => _service = service;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin =>
        User.IsInRole("Admin");

    /// <summary>Create a new booking for a vehicle.</summary>
    /// <param name="req">Booking details including vehicle ID, dates, and price per day.</param>
    [HttpPost]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest req)
    {
        try
        {
            var result = await _service.CreateAsync(req, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get a booking by ID. Users can only see their own bookings.</summary>
    /// <param name="id">Booking GUID.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null) return NotFound();
        if (!IsAdmin && result.UserId != CurrentUserId) return Forbid();
        return Ok(result);
    }

    /// <summary>Get all bookings for the current user.</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(List<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBookings()
    {
        var result = await _service.GetUserBookingsAsync(CurrentUserId);
        return Ok(result);
    }

    /// <summary>Get all bookings. Requires Admin role.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    /// <summary>Cancel a booking.</summary>
    /// <param name="id">Booking GUID.</param>
    /// <param name="req">Cancellation reason.</param>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelBookingRequest req)
    {
        try
        {
            var result = await _service.CancelAsync(id, CurrentUserId, IsAdmin, req.Reason);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
