using TravelSystem.Domain.Enums;

namespace TravelSystem.Domain.Entities;

public class Itinerary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Destination { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? Budget { get; set; }
    public string? CurrencyCode { get; set; } = "AOA";
    public ItineraryStatus Status { get; set; } = ItineraryStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<ItineraryStop> Stops { get; set; } = [];
    public ICollection<ItineraryAttraction> Attractions { get; set; } = [];
    public ICollection<AiChatMessage> ChatHistory { get; set; } = [];
    public ICollection<ItineraryExpense> Expenses { get; set; } = [];
}

public class ItineraryStop
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItineraryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int DayNumber { get; set; }
    public int OrderIndex { get; set; }
    public string? Notes { get; set; }
    public StopCategory Category { get; set; } = StopCategory.Other;
    public DateTime? VisitTime { get; set; }
    public int? DurationMinutes { get; set; }

    // Navigation
    public Itinerary Itinerary { get; set; } = null!;
}

public class ItineraryAttraction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItineraryId { get; set; }
    public string PlaceId { get; set; } = string.Empty; // Google Maps Place ID
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? ImageUrl { get; set; }
    public double? Rating { get; set; }
    public bool IsVisited { get; set; } = false;

    // Navigation
    public Itinerary Itinerary { get; set; } = null!;
}

public class ItineraryExpense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItineraryId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "AOA";
    public DateTime Date { get; set; }

    // Navigation
    public Itinerary Itinerary { get; set; } = null!;
}

public class AiChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItineraryId { get; set; }
    public string Role { get; set; } = string.Empty; // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Itinerary Itinerary { get; set; } = null!;
}
