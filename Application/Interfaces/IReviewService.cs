using AirbnbClone.Api.Application.DTOs;

namespace AirbnbClone.Api.Application.Interfaces;

public interface IReviewService
{
    // Guest: konaklama yorumu
    Task<ReviewDTO> ReviewStayAsync(Guid guestId, ReviewDTO request, CancellationToken cancellationToken = default);
}
