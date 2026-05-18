using TravelSystem.Application.DTOs.Itinerary;
using TravelSystem.Application.Interfaces;
using TravelSystem.Domain.Entities;
using TravelSystem.Domain.Interfaces;

namespace TravelSystem.Application.Services;

public class ItineraryService : IItineraryService
{
    private readonly IUnitOfWork _uow;

    public ItineraryService(IUnitOfWork uow) => _uow = uow;

    public async Task<ItineraryDto> CreateAsync(Guid userId, CreateItineraryRequest request, CancellationToken ct = default)
    {
        var itinerary = new Itinerary
        {
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Destination = request.Destination,
            CountryCode = request.CountryCode,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Budget = request.Budget,
            CurrencyCode = request.CurrencyCode ?? "AOA"
        };

        await _uow.Itineraries.AddAsync(itinerary, ct);
        await _uow.CommitAsync(ct);
        return MapToDto(itinerary);
    }

    public async Task<ItineraryDetailDto> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var itinerary = await _uow.Itineraries.GetFullItineraryAsync(id, ct)
            ?? throw new KeyNotFoundException("ITINERARY_NOT_FOUND");

        if (itinerary.UserId != userId)
            throw new UnauthorizedAccessException("ACCESS_DENIED");

        return MapToDetailDto(itinerary);
    }

    public async Task<IEnumerable<ItineraryDto>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var itineraries = await _uow.Itineraries.GetByUserIdAsync(userId, ct);
        return itineraries.Select(MapToDto);
    }

    public async Task<ItineraryDto> UpdateAsync(Guid id, Guid userId, UpdateItineraryRequest request, CancellationToken ct = default)
    {
        var itinerary = await _uow.Itineraries.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("ITINERARY_NOT_FOUND");

        if (itinerary.UserId != userId)
            throw new UnauthorizedAccessException("ACCESS_DENIED");

        itinerary.Title = request.Title;
        itinerary.Description = request.Description;
        itinerary.Destination = request.Destination;
        itinerary.StartDate = request.StartDate;
        itinerary.EndDate = request.EndDate;
        itinerary.Budget = request.Budget;
        itinerary.CurrencyCode = request.CurrencyCode;
        itinerary.Status = request.Status;
        itinerary.UpdatedAt = DateTime.UtcNow;

        await _uow.Itineraries.UpdateAsync(itinerary, ct);
        await _uow.CommitAsync(ct);
        return MapToDto(itinerary);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var itinerary = await _uow.Itineraries.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("ITINERARY_NOT_FOUND");

        if (itinerary.UserId != userId)
            throw new UnauthorizedAccessException("ACCESS_DENIED");

        await _uow.Itineraries.DeleteAsync(itinerary, ct);
        await _uow.CommitAsync(ct);
    }

    public async Task<ItineraryStopDto> AddStopAsync(Guid itineraryId, Guid userId, CreateStopRequest request, CancellationToken ct = default)
    {
        var itinerary = await _uow.Itineraries.GetByIdAsync(itineraryId, ct)
            ?? throw new KeyNotFoundException("ITINERARY_NOT_FOUND");

        if (itinerary.UserId != userId)
            throw new UnauthorizedAccessException("ACCESS_DENIED");

        var stop = new ItineraryStop
        {
            ItineraryId = itineraryId,
            Name = request.Name,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            DayNumber = request.DayNumber,
            OrderIndex = request.OrderIndex,
            Notes = request.Notes,
            Category = request.Category,
            VisitTime = request.VisitTime,
            DurationMinutes = request.DurationMinutes
        };

        itinerary.Stops.Add(stop);
        itinerary.UpdatedAt = DateTime.UtcNow;
        await _uow.CommitAsync(ct);

        return MapStopToDto(stop);
    }

    public async Task DeleteStopAsync(Guid itineraryId, Guid stopId, Guid userId, CancellationToken ct = default)
    {
        var itinerary = await _uow.Itineraries.GetWithStopsAsync(itineraryId, ct)
            ?? throw new KeyNotFoundException("ITINERARY_NOT_FOUND");

        if (itinerary.UserId != userId)
            throw new UnauthorizedAccessException("ACCESS_DENIED");

        var stop = itinerary.Stops.FirstOrDefault(s => s.Id == stopId)
            ?? throw new KeyNotFoundException("STOP_NOT_FOUND");

        itinerary.Stops.Remove(stop);
        itinerary.UpdatedAt = DateTime.UtcNow;
        await _uow.CommitAsync(ct);
    }

    public async Task<ItineraryStopDto> ReorderStopAsync(Guid itineraryId, Guid stopId, Guid userId, int newOrder, CancellationToken ct = default)
    {
        var itinerary = await _uow.Itineraries.GetWithStopsAsync(itineraryId, ct)
            ?? throw new KeyNotFoundException("ITINERARY_NOT_FOUND");

        if (itinerary.UserId != userId)
            throw new UnauthorizedAccessException("ACCESS_DENIED");

        var stop = itinerary.Stops.FirstOrDefault(s => s.Id == stopId)
            ?? throw new KeyNotFoundException("STOP_NOT_FOUND");

        stop.OrderIndex = newOrder;
        itinerary.UpdatedAt = DateTime.UtcNow;
        await _uow.CommitAsync(ct);

        return MapStopToDto(stop);
    }

    // ── Mappers ──────────────────────────────────────────────────────────────

    private static ItineraryDto MapToDto(Itinerary i) =>
        new(i.Id, i.Title, i.Description, i.Destination, i.CountryCode,
            i.Latitude, i.Longitude, i.StartDate, i.EndDate, i.Budget, i.CurrencyCode,
            i.Status, (i.EndDate - i.StartDate).Days + 1, i.Stops.Count,
            i.CreatedAt, i.UpdatedAt);

    private static ItineraryDetailDto MapToDetailDto(Itinerary i)
    {
        var totalSpent = i.Expenses.Sum(e => e.Amount);
        return new(
            i.Id, i.Title, i.Description, i.Destination, i.Latitude, i.Longitude,
            i.StartDate, i.EndDate, i.Budget, i.CurrencyCode, i.Status,
            i.Stops.OrderBy(s => s.DayNumber).ThenBy(s => s.OrderIndex).Select(MapStopToDto).ToList(),
            i.Attractions.Select(a => new ItineraryAttractionDto(
                a.Id, a.PlaceId, a.Name, a.Category, a.Latitude, a.Longitude,
                a.ImageUrl, a.Rating, a.IsVisited)).ToList(),
            i.Expenses.Select(e => new ItineraryExpenseDto(
                e.Id, e.Category, e.Description, e.Amount, e.CurrencyCode, e.Date)).ToList(),
            totalSpent
        );
    }

    private static ItineraryStopDto MapStopToDto(ItineraryStop s) =>
        new(s.Id, s.Name, s.Address, s.Latitude, s.Longitude, s.DayNumber,
            s.OrderIndex, s.Notes, s.Category, s.VisitTime, s.DurationMinutes);
}
