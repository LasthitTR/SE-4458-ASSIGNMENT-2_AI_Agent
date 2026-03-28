using AirbnbClone.Api.Application.DTOs;

namespace AirbnbClone.Api.Application.Interfaces;

public interface IListingService
{
    // Host: ilan ekleme
    Task<ListingResponseDTO> InsertListingAsync(Guid hostId, ListingCreateDTO request, CancellationToken cancellationToken = default);

    // Guest: ilan sorgulama (sayfalama + rezerve tarih filtreleme)
    Task<PagedResultDTO<ListingResponseDTO>> QueryListingsAsync(ListingQueryDTO query, CancellationToken cancellationToken = default);
}
