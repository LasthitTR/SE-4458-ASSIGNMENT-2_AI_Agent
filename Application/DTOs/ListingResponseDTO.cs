namespace AirbnbClone.Api.Application.DTOs;

public class ListingResponseDTO
{
    public Guid Id { get; set; }
    public Guid HostId { get; set; }
    public string HostFullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal Price { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
