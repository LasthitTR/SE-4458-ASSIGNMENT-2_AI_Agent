using System.Security.Claims;
using AirbnbClone.Api.Application.DTOs;
using AirbnbClone.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirbnbClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;

    public ListingsController(IListingService listingService)
    {
        _listingService = listingService;
    }

    [Authorize(Roles = "Host,Admin")]
    [HttpPost]
    public async Task<ActionResult<ListingResponseDTO>> Create([FromBody] ListingCreateDTO request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var hostId))
        {
            return Unauthorized("Invalid token user id.");
        }

        try
        {
            var result = await _listingService.InsertListingAsync(hostId, request, cancellationToken);
            return CreatedAtAction(nameof(Search), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResultDTO<ListingResponseDTO>>> Search([FromQuery] ListingQueryDTO query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _listingService.QueryListingsAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
