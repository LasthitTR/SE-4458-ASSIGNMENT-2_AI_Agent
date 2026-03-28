using AirbnbClone.Api.Application.DTOs;
using AirbnbClone.Api.Application.Interfaces;
using AirbnbClone.Api.Domain.Entities;
using AirbnbClone.Api.Domain.Enums;
using AirbnbClone.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirbnbClone.Api.Application.Services;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _dbContext;

    public ReviewService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReviewDTO> ReviewStayAsync(Guid guestId, ReviewDTO request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Rating is < 1 or > 5)
        {
            throw new InvalidOperationException("Rating must be between 1 and 5.");
        }

        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new InvalidOperationException("Comment is required.");
        }

        // BookingId'nin yorumu yapan guest'e ait oldugu kesin kontrol.
        var booking = await _dbContext.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.BookingId && x.GuestId == guestId, cancellationToken);

        if (booking is null)
        {
            throw new InvalidOperationException("Booking not found for the current guest.");
        }

        if (booking.ListingId != request.ListingId)
        {
            throw new InvalidOperationException("Booking does not belong to the provided listing.");
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled bookings cannot be reviewed.");
        }

        if (booking.ToDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Review can be added only after stay is completed.");
        }

        var alreadyReviewed = await _dbContext.Reviews
            .AsNoTracking()
            .AnyAsync(x => x.BookingId == request.BookingId, cancellationToken);

        if (alreadyReviewed)
        {
            throw new InvalidOperationException("This booking already has a review.");
        }

        var review = new Review
        {
            Id = Guid.NewGuid(),
            BookingId = request.BookingId,
            ListingId = request.ListingId,
            GuestId = guestId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ReviewDTO
        {
            BookingId = review.BookingId,
            ListingId = review.ListingId,
            Rating = review.Rating,
            Comment = review.Comment
        };
    }
}
