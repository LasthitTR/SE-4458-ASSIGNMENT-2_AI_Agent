using AirbnbClone.Api.Application.DTOs;

namespace AirbnbClone.Api.Application.Interfaces;

public interface IAdminService
{
    // Admin: ilanlari rating bilgisiyle raporlama (sayfalama dahil)
    Task<PagedResultDTO<ListingRatingReportDTO>> ReportListingsWithRatingsAsync(
        ListingRatingReportQueryDTO query,
        CancellationToken cancellationToken = default);

    // Admin: CSV dosyasindan toplu ilan ekleme
    Task<BulkInsertResultDTO> InsertListingByFileAsync(
        Stream csvFileStream,
        CancellationToken cancellationToken = default);
}
