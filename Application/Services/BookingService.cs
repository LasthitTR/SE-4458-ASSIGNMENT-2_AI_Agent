using AirbnbClone.Api.Application.DTOs;
using AirbnbClone.Api.Application.Interfaces;
using AirbnbClone.Api.Domain.Entities;
using AirbnbClone.Api.Domain.Enums;
using AirbnbClone.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirbnbClone.Api.Application.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _dbContext;

    public BookingService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BookingResponseDTO> BookStayAsync(Guid guestId, BookStayDTO request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.FromDate >= request.ToDate)
        {
            throw new InvalidOperationException("FromDate must be earlier than ToDate.");
        }

        if (request.NoOfPeople <= 0)
        {
            throw new InvalidOperationException("NoOfPeople must be greater than zero.");
        }

        var guest = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == guestId && x.IsActive, cancellationToken);

        if (guest is null)
        {
            throw new InvalidOperationException("Guest user not found.");
        }

        if (guest.Role is not UserRole.Guest and not UserRole.Admin)
        {
            throw new InvalidOperationException("Only guest users can create bookings.");
        }

        var listing = await _dbContext.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ListingId && x.IsActive, cancellationToken);

        if (listing is null)
        {
            throw new InvalidOperationException("Listing not found.");
        }

        if (request.NoOfPeople > listing.Capacity)
        {
            throw new InvalidOperationException("NoOfPeople cannot exceed listing capacity.");
        }

        // Rezervasyon aninda tekrar cakisma kontrolu.
        var hasOverlap = await _dbContext.Bookings
            .AsNoTracking()
            .AnyAsync(x =>
                x.ListingId == request.ListingId &&
                x.Status != BookingStatus.Cancelled &&
                x.FromDate < request.ToDate &&
                request.FromDate < x.ToDate,
                cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException("Listing is not available for the requested date range.");
        }

        var totalNights = request.ToDate.DayNumber - request.FromDate.DayNumber;
        var totalPrice = totalNights * listing.Price;

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            GuestId = guestId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            NoOfPeople = request.NoOfPeople,
            TotalPrice = totalPrice,
            Status = BookingStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new BookingResponseDTO
        {
            Id = booking.Id,
            ListingId = booking.ListingId,
            GuestId = booking.GuestId,
            FromDate = booking.FromDate,
            ToDate = booking.ToDate,
            NoOfPeople = booking.NoOfPeople,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status
        };
    }
}
