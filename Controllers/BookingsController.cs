using System.Security.Claims;
using AirbnbClone.Api.Application.DTOs;
using AirbnbClone.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirbnbClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [Authorize(Roles = "Guest")]
    [HttpPost]
    public async Task<ActionResult<BookingResponseDTO>> Book([FromBody] BookStayDTO request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var guestId))
        {
            return Unauthorized("Invalid token user id.");
        }

        try
        {
            var result = await _bookingService.BookStayAsync(guestId, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
