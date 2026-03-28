namespace AirbnbClone.Api.Application.DTOs;

public class ListingQueryDTO
{
    public string? Country { get; set; }
    public string? City { get; set; }
    public int? Capacity { get; set; }

    // Rezerve tarihlerle cakismayan ilanlari getirmek icin filtre.
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
