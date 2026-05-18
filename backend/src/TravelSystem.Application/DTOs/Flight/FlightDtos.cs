namespace TravelSystem.Application.DTOs.Flight;

public record FlightSearchRequest(
    string? OriginCode,
    string? DestinationCode,
    DateTime DepartureDate,
    int Passengers = 1,
    string CabinClass = "economy",
    string? SortBy = "price",
    string? Origin = null,
    string? Destination = null,
    DateTime? ReturnDate = null
)
{
    public string EffectiveOrigin => (OriginCode ?? Origin ?? string.Empty).ToUpperInvariant();
    public string EffectiveDestination => (DestinationCode ?? Destination ?? string.Empty).ToUpperInvariant();
}

public record FlightDto(
    Guid Id,
    string ExternalId,
    string Provider,
    string Airline,
    string FlightNumber,
    string OriginCode,
    string DestinationCode,
    string OriginCity,
    string DestinationCity,
    DateTime DepartureAt,
    DateTime ArrivalAt,
    int DurationMinutes,
    int Stops,
    string CabinClass,
    decimal Price,
    string CurrencyCode,
    int SeatsAvailable
)
{
    public string Origin => OriginCode;
    public string Destination => DestinationCode;
    public string Currency => CurrencyCode;
}

public record CreateFlightAlertRequest(
    string? OriginCode,
    string? DestinationCode,
    DateTime? DepartureFrom,
    DateTime? DepartureTo,
    decimal TargetPrice,
    string CurrencyCode = "USD",
    string? Origin = null,
    string? Destination = null,
    bool Enabled = true
)
{
    public string EffectiveOrigin => (OriginCode ?? Origin ?? string.Empty).ToUpperInvariant();
    public string EffectiveDestination => (DestinationCode ?? Destination ?? string.Empty).ToUpperInvariant();
}

public record FlightAlertDto(
    Guid Id,
    string OriginCode,
    string DestinationCode,
    DateTime? DepartureFrom,
    DateTime? DepartureTo,
    decimal TargetPrice,
    string CurrencyCode,
    bool IsActive,
    DateTime? LastTriggeredAt,
    DateTime CreatedAt
)
{
    public string Origin => OriginCode;
    public string Destination => DestinationCode;
    public bool Enabled => IsActive;
}
