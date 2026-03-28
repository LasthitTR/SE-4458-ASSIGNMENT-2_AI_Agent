using AirbnbClone.Api.Domain.Enums;

namespace AirbnbClone.Api.Application.DTOs;

public class BookingResponseDTO
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid GuestId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int NoOfPeople { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; }
}
