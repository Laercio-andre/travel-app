using TravelSystem.Domain.Enums;

namespace TravelSystem.Application.DTOs.Hotel;

public record HotelSearchRequest(
    string City,
    DateTime CheckIn,
    DateTime CheckOut,
    int Guests = 1,
    int? MinStars = null,
    decimal? MaxPrice = null,
    string? SortBy = "price"
);

public record HotelDto(
    Guid Id,
    string ExternalId,
    string Provider,
    string Name,
    string Address,
    string City,
    string CountryCode,
    double Latitude,
    double Longitude,
    int StarRating,
    double? GuestRating,
    string? ImageUrl,
    string? Description,
    List<string> Amenities,
    decimal? LowestPrice,
    string? CurrencyCode
)
{
    public string Country => CountryCode;
    public decimal PricePerNight => LowestPrice ?? 0;
    public double? Rating => GuestRating;
}

public record HotelDetailDto(
    Guid Id,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    int StarRating,
    double? GuestRating,
    string? Description,
    List<string> Amenities,
    List<HotelRoomDto> Rooms
);

public record HotelRoomDto(
    Guid Id,
    string RoomType,
    string? Description,
    int MaxGuests,
    decimal PricePerNight,
    string CurrencyCode,
    bool IsAvailable
);

public record CreateBookingRequest(
    Guid? HotelId,
    Guid? HotelRoomId,
    Guid? FlightId,
    BookingType Type = BookingType.Hotel,
    DateTime? CheckIn = null,
    DateTime? CheckOut = null,
    int Guests = 1,
    decimal TotalPrice = 0,
    string CurrencyCode = "USD",
    string? Notes = null
);

public record BookingDto(
    Guid Id,
    BookingType Type,
    BookingStatus Status,
    DateTime CheckIn,
    DateTime CheckOut,
    int Guests,
    decimal TotalPrice,
    string CurrencyCode,
    string? ConfirmationNumber,
    DateTime CreatedAt,
    string? HotelName,
    string? FlightNumber
)
{
    public string StatusText => Status.ToString();
}
