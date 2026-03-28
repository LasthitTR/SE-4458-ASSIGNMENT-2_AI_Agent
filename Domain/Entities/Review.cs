namespace AirbnbClone.Api.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }

    // Sadece yapilan konaklama icin yorum: booking baglantisi zorunlu.
    public Guid BookingId { get; set; }
    public Guid ListingId { get; set; }
    public Guid GuestId { get; set; }

    // 1-5 puan araligi Fluent API ile sinirlanir.
    public byte Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Booking Booking { get; set; } = null!;
    public Listing Listing { get; set; } = null!;
    public User Guest { get; set; } = null!;
}
