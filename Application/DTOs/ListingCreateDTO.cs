namespace AirbnbClone.Api.Application.DTOs;

public class ListingCreateDTO
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal Price { get; set; }
}
