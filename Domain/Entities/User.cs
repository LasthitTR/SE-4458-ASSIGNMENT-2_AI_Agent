using AirbnbClone.Api.Domain.Enums;

namespace AirbnbClone.Api.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Host tarafi: bir host birden fazla ilan ekleyebilir.
    public ICollection<Listing> HostListings { get; set; } = new List<Listing>();

    // Guest tarafi: bir guest birden fazla rezervasyon yapabilir.
    public ICollection<Booking> GuestBookings { get; set; } = new List<Booking>();

    // Guest tarafi: konaklama sonrasinda yorumlar.
    public ICollection<Review> ReviewsWritten { get; set; } = new List<Review>();
}
