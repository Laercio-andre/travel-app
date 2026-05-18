using TravelSystem.Domain.Enums;

namespace TravelSystem.Application.DTOs.Itinerary;

public record CreateItineraryRequest(
    string Title,
    string? Description,
    string Destination,
    string? CountryCode,
    double? Latitude,
    double? Longitude,
    DateTime StartDate,
    DateTime EndDate,
    decimal? Budget,
    string? CurrencyCode
);

public record UpdateItineraryRequest(
    string Title,
    string? Description,
    string Destination,
    DateTime StartDate,
    DateTime EndDate,
    decimal? Budget,
    string? CurrencyCode,
    ItineraryStatus Status
);

public record ItineraryDto(
    Guid Id,
    string Title,
    string? Description,
    string Destination,
    string? CountryCode,
    double? Latitude,
    double? Longitude,
    DateTime StartDate,
    DateTime EndDate,
    decimal? Budget,
    string? CurrencyCode,
    ItineraryStatus Status,
    int TotalDays,
    int StopsCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ItineraryDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string Destination,
    double? Latitude,
    double? Longitude,
    DateTime StartDate,
    DateTime EndDate,
    decimal? Budget,
    string? CurrencyCode,
    ItineraryStatus Status,
    List<ItineraryStopDto> Stops,
    List<ItineraryAttractionDto> Attractions,
    List<ItineraryExpenseDto> Expenses,
    decimal TotalSpent
);

public record CreateStopRequest(
    string Name,
    string? Address,
    double Latitude,
    double Longitude,
    int DayNumber,
    int OrderIndex,
    string? Notes,
    StopCategory Category,
    DateTime? VisitTime,
    int? DurationMinutes
);

public record ItineraryStopDto(
    Guid Id,
    string Name,
    string? Address,
    double Latitude,
    double Longitude,
    int DayNumber,
    int OrderIndex,
    string? Notes,
    StopCategory Category,
    DateTime? VisitTime,
    int? DurationMinutes
);

public record ItineraryAttractionDto(
    Guid Id,
    string PlaceId,
    string Name,
    string? Category,
    double Latitude,
    double Longitude,
    string? ImageUrl,
    double? Rating,
    bool IsVisited
);

public record ItineraryExpenseDto(
    Guid Id,
    string Category,
    string Description,
    decimal Amount,
    string CurrencyCode,
    DateTime Date
);
