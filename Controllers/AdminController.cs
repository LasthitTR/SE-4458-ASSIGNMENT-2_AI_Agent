using AirbnbClone.Api.Application.DTOs;
using AirbnbClone.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirbnbClone.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("reports")]
    public async Task<ActionResult<PagedResultDTO<ListingRatingReportDTO>>> Reports(
        [FromQuery] ListingRatingReportQueryDTO query,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.ReportListingsWithRatingsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BulkInsertResultDTO>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("CSV file is required.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only .csv files are allowed.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _adminService.InsertListingByFileAsync(stream, cancellationToken);

        return Ok(result);
    }
}
