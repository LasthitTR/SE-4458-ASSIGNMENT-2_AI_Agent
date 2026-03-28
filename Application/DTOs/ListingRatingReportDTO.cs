namespace AirbnbClone.Api.Application.DTOs;

public class ListingRatingReportDTO
{
    public Guid ListingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
