namespace AirbnbClone.Api.Application.DTOs;

public class ReviewDTO
{
    public Guid BookingId { get; set; }
    public Guid ListingId { get; set; }
    public byte Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
