using System.Security.Claims;
using AirbnbClone.Api.Application.DTOs;
using AirbnbClone.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirbnbClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [Authorize(Roles = "Guest")]
    [HttpPost]
    public async Task<ActionResult<ReviewDTO>> Create([FromBody] ReviewDTO request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var guestId))
        {
            return Unauthorized("Invalid token user id.");
        }

        try
        {
            var result = await _reviewService.ReviewStayAsync(guestId, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
