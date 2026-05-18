using TravelSystem.Domain.Enums;

namespace TravelSystem.Domain.Entities;

public class Hotel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ExternalId { get; set; } = string.Empty; // Provider's ID
    public string Provider { get; set; } = string.Empty; // "booking", "hotels_com"
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int StarRating { get; set; }
    public double? GuestRating { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public List<string> Amenities { get; set; } = [];
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<HotelRoom> Rooms { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
}

public class HotelRoom
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HotelId { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxGuests { get; set; }
    public decimal PricePerNight { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsAvailable { get; set; } = true;

    // Navigation
    public Hotel Hotel { get; set; } = null!;
}

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? HotelId { get; set; }
    public Guid? HotelRoomId { get; set; }
    public Guid? FlightId { get; set; }
    public BookingType Type { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Guests { get; set; } = 1;
    public decimal TotalPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? ConfirmationNumber { get; set; }
    public string? ProviderReference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Hotel? Hotel { get; set; }
    public HotelRoom? HotelRoom { get; set; }
    public Flight? Flight { get; set; }
}

public class Flight
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ExternalId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty; // "amadeus", "skyscanner"
    public string Airline { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
    public string OriginCode { get; set; } = string.Empty;
    public string DestinationCode { get; set; } = string.Empty;
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime DepartureAt { get; set; }
    public DateTime ArrivalAt { get; set; }
    public int DurationMinutes { get; set; }
    public int Stops { get; set; } = 0;
    public string CabinClass { get; set; } = "economy";
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int SeatsAvailable { get; set; }
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Booking> Bookings { get; set; } = [];
}

public class FlightAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string OriginCode { get; set; } = string.Empty;
    public string DestinationCode { get; set; } = string.Empty;
    public DateTime? DepartureFrom { get; set; }
    public DateTime? DepartureTo { get; set; }
    public decimal TargetPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
