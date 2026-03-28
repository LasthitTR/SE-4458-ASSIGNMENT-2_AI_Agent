using AirbnbClone.Api.Application.DTOs;
using AirbnbClone.Api.Application.Interfaces;
using AirbnbClone.Api.Domain.Entities;
using AirbnbClone.Api.Domain.Enums;
using AirbnbClone.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirbnbClone.Api.Application.Services;

public class ListingService : IListingService
{
    private readonly AppDbContext _dbContext;

    public ListingService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ListingResponseDTO> InsertListingAsync(Guid hostId, ListingCreateDTO request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Capacity <= 0)
        {
            throw new InvalidOperationException("Capacity must be greater than zero.");
        }

        if (request.Price < 0)
        {
            throw new InvalidOperationException("Price cannot be negative.");
        }

        var host = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == hostId && x.IsActive, cancellationToken);

        if (host is null)
        {
            throw new InvalidOperationException("Host user not found.");
        }

        if (host.Role is not UserRole.Host and not UserRole.Admin)
        {
            throw new InvalidOperationException("Only host users can insert listing.");
        }

        var now = DateTime.UtcNow;

        var entity = new Listing
        {
            Id = Guid.NewGuid(),
            HostId = hostId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Country = request.Country.Trim(),
            City = request.City.Trim(),
            AddressLine = request.AddressLine.Trim(),
            Capacity = request.Capacity,
            Price = request.Price,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Listings.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ListingResponseDTO
        {
            Id = entity.Id,
            HostId = entity.HostId,
            HostFullName = $"{host.FirstName} {host.LastName}".Trim(),
            Title = entity.Title,
            Description = entity.Description,
            Country = entity.Country,
            City = entity.City,
            AddressLine = entity.AddressLine,
            Capacity = entity.Capacity,
            Price = entity.Price,
            AverageRating = null,
            ReviewCount = 0
        };
    }

    public async Task<PagedResultDTO<ListingResponseDTO>> QueryListingsAsync(ListingQueryDTO query, CancellationToken cancellationToken = default)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate.Value >= query.ToDate.Value)
        {
            throw new InvalidOperationException("FromDate must be earlier than ToDate.");
        }

        IQueryable<Listing> listingQuery = _dbContext.Listings
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            var country = query.Country.Trim();
            listingQuery = listingQuery.Where(x => x.Country == country);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            listingQuery = listingQuery.Where(x => x.City == city);
        }

        if (query.Capacity.HasValue)
        {
            listingQuery = listingQuery.Where(x => x.Capacity >= query.Capacity.Value);
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue)
        {
            var fromDate = query.FromDate.Value;
            var toDate = query.ToDate.Value;

            // Kesisen rezervasyon varsa ilani disla.
            listingQuery = listingQuery.Where(listing => !listing.Bookings.Any(booking =>
                booking.Status != BookingStatus.Cancelled &&
                booking.FromDate < toDate &&
                fromDate < booking.ToDate));
        }

        var totalCount = await listingQuery.CountAsync(cancellationToken);

        var items = await listingQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ListingResponseDTO
            {
                Id = x.Id,
                HostId = x.HostId,
                HostFullName = x.Host.FirstName + " " + x.Host.LastName,
                Title = x.Title,
                Description = x.Description,
                Country = x.Country,
                City = x.City,
                AddressLine = x.AddressLine,
                Capacity = x.Capacity,
                Price = x.Price,
                AverageRating = x.Reviews.Any() ? x.Reviews.Average(r => (double)r.Rating) : null,
                ReviewCount = x.Reviews.Count
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDTO<ListingResponseDTO>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
