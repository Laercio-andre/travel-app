using Microsoft.AspNetCore.Identity;

namespace TravelSystem.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "pt-AO";
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // Navigation
    public ICollection<Itinerary> Itineraries { get; set; } = [];
    public ICollection<FlightAlert> FlightAlerts { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
}
