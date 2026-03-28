using AirbnbClone.Api.Application.DTOs;

namespace AirbnbClone.Api.Application.Interfaces;

public interface IBookingService
{
    // Guest: konaklama rezervasyonu
    Task<BookingResponseDTO> BookStayAsync(Guid guestId, BookStayDTO request, CancellationToken cancellationToken = default);
}
