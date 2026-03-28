using AirbnbClone.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AirbnbClone.Api.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureListing(modelBuilder);
        ConfigureBooking(modelBuilder);
        ConfigureReview(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(x => x.Role)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.HasMany(x => x.HostListings)
                .WithOne(x => x.Host)
                .HasForeignKey(x => x.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.GuestBookings)
                .WithOne(x => x.Guest)
                .HasForeignKey(x => x.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.ReviewsWritten)
                .WithOne(x => x.Guest)
                .HasForeignKey(x => x.GuestId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureListing(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Listing>(entity =>
        {
            entity.ToTable("Listings", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("CK_Listings_Capacity_Positive", "[Capacity] > 0");
                tableBuilder.HasCheckConstraint("CK_Listings_Price_NonNegative", "[Price] >= 0");
            });

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(x => x.Country)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.City)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.AddressLine)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(x => x.Capacity)
                .IsRequired();

            entity.Property(x => x.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.Property(x => x.UpdatedAtUtc)
                .IsRequired();

            entity.HasMany(x => x.Bookings)
                .WithOne(x => x.Listing)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Reviews)
                .WithOne(x => x.Listing)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureBooking(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Bookings", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("CK_Bookings_Date_Order", "[FromDate] < [ToDate]");
                tableBuilder.HasCheckConstraint("CK_Bookings_NoOfPeople_Positive", "[NoOfPeople] > 0");
                tableBuilder.HasCheckConstraint("CK_Bookings_TotalPrice_NonNegative", "[TotalPrice] >= 0");
            });

            entity.HasKey(x => x.Id);

            entity.Property(x => x.FromDate)
                .IsRequired();

            entity.Property(x => x.ToDate)
                .IsRequired();

            entity.Property(x => x.NoOfPeople)
                .IsRequired();

            entity.Property(x => x.TotalPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.Status)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.HasOne(x => x.Review)
                .WithOne(x => x.Booking)
                .HasForeignKey<Review>(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ListingId, x.FromDate, x.ToDate });
        });
    }

    private static void ConfigureReview(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("Reviews", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("CK_Reviews_Rating_Range", "[Rating] >= 1 AND [Rating] <= 5");
            });

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Rating)
                .IsRequired();

            entity.Property(x => x.Comment)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            // Bir booking icin en fazla bir review.
            entity.HasIndex(x => x.BookingId)
                .IsUnique();
        });
    }
}
