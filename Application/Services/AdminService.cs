using System.Globalization;
using AirbnbClone.Api.Application.DTOs;
using AirbnbClone.Api.Application.Interfaces;
using AirbnbClone.Api.Domain.Entities;
using AirbnbClone.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirbnbClone.Api.Application.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _dbContext;

    public AdminService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResultDTO<ListingRatingReportDTO>> ReportListingsWithRatingsAsync(
        ListingRatingReportQueryDTO query,
        CancellationToken cancellationToken = default)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        var reportQuery = _dbContext.Listings
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new ListingRatingReportDTO
            {
                ListingId = x.Id,
                Title = x.Title,
                Country = x.Country,
                City = x.City,
                Price = x.Price,
                AverageRating = x.Reviews.Any() ? x.Reviews.Average(r => (double)r.Rating) : 0,
                ReviewCount = x.Reviews.Count
            });

        var totalCount = await reportQuery.CountAsync(cancellationToken);

        var items = await reportQuery
            .OrderByDescending(x => x.AverageRating)
            .ThenByDescending(x => x.ReviewCount)
            .ThenBy(x => x.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDTO<ListingRatingReportDTO>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BulkInsertResultDTO> InsertListingByFileAsync(
        Stream csvFileStream,
        CancellationToken cancellationToken = default)
    {
        if (csvFileStream is null)
        {
            throw new ArgumentNullException(nameof(csvFileStream));
        }

        var errors = new List<string>();
        var listings = new List<Listing>();
        var totalDataRows = 0;

        if (csvFileStream.CanSeek)
        {
            csvFileStream.Position = 0;
        }

        using var reader = new StreamReader(csvFileStream, leaveOpen: true);

        var lineNumber = 0;

        // Beklenen sutunlar:
        // HostId,Title,Description,Country,City,AddressLine,Capacity,Price
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rawLine = await reader.ReadLineAsync(cancellationToken);
            lineNumber++;

            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var columns = ParseCsvLine(rawLine);

            if (lineNumber == 1 && columns.Count > 0 && columns[0].Equals("HostId", StringComparison.OrdinalIgnoreCase))
            {
                // Header satiri
                continue;
            }

            totalDataRows++;

            if (columns.Count < 8)
            {
                errors.Add($"Line {lineNumber}: expected 8 columns but got {columns.Count}.");
                continue;
            }

            if (!Guid.TryParse(columns[0], out var hostId))
            {
                errors.Add($"Line {lineNumber}: invalid HostId.");
                continue;
            }

            if (!int.TryParse(columns[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var capacity) || capacity <= 0)
            {
                errors.Add($"Line {lineNumber}: invalid Capacity.");
                continue;
            }

            if (!decimal.TryParse(columns[7], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0)
            {
                errors.Add($"Line {lineNumber}: invalid Price.");
                continue;
            }

            listings.Add(new Listing
            {
                Id = Guid.NewGuid(),
                HostId = hostId,
                Title = columns[1].Trim(),
                Description = columns[2].Trim(),
                Country = columns[3].Trim(),
                City = columns[4].Trim(),
                AddressLine = columns[5].Trim(),
                Capacity = capacity,
                Price = price,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        // Sadece host rolundeki aktif kullanicilarin listing eklemesine izin ver.
        var validHostIds = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.IsActive && x.Role == Domain.Enums.UserRole.Host)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var validHostSet = validHostIds.ToHashSet();

        var validListings = new List<Listing>();
        foreach (var listing in listings)
        {
            if (!validHostSet.Contains(listing.HostId))
            {
                errors.Add($"Listing '{listing.Title}': HostId is not an active host.");
                continue;
            }

            validListings.Add(listing);
        }

        if (validListings.Count > 0)
        {
            await _dbContext.Listings.AddRangeAsync(validListings, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new BulkInsertResultDTO
        {
            TotalRows = totalDataRows,
            InsertedRows = validListings.Count,
            FailedRows = totalDataRows - validListings.Count,
            Errors = errors
        };
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString());
        return result;
    }
}
