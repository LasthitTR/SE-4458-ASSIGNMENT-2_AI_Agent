using AirbnbClone.Api.Domain.Enums;

namespace AirbnbClone.Api.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid GuestId { get; set; }

    // Sinav gereksinimleri: FromDate, ToDate, NoOfPeople.
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int NoOfPeople { get; set; }

    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Listing Listing { get; set; } = null!;
    public User Guest { get; set; } = null!;
    public Review? Review { get; set; }
}
