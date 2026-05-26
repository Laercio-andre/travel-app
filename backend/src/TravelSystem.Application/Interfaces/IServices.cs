using TravelSystem.Application.DTOs.Auth;
using TravelSystem.Application.DTOs.Itinerary;
using TravelSystem.Application.DTOs.Hotel;
using TravelSystem.Application.DTOs.Flight;
using TravelSystem.Application.DTOs.AI;
using TravelSystem.Application.DTOs.Report;

namespace TravelSystem.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task LogoutAsync(Guid userId, CancellationToken ct = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<string?> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<IEnumerable<UserProfileDto>> GetAllUsersAsync(CancellationToken ct = default); // Admin only
    Task<bool> DeactivateUserAsync(Guid userId, CancellationToken ct = default); // Admin only
}

public interface IItineraryService
{
    Task<ItineraryDto> CreateAsync(Guid userId, CreateItineraryRequest request, CancellationToken ct = default);
    Task<ItineraryDetailDto> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<ItineraryDto>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<ItineraryDto> UpdateAsync(Guid id, Guid userId, UpdateItineraryRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<ItineraryStopDto> AddStopAsync(Guid itineraryId, Guid userId, CreateStopRequest request, CancellationToken ct = default);
    Task DeleteStopAsync(Guid itineraryId, Guid stopId, Guid userId, CancellationToken ct = default);
    Task<ItineraryStopDto> ReorderStopAsync(Guid itineraryId, Guid stopId, Guid userId, int newOrder, CancellationToken ct = default);
}

public interface IHotelService
{
    Task<IEnumerable<HotelDto>> SearchAsync(HotelSearchRequest request, CancellationToken ct = default);
    Task<HotelDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BookingDto> BookAsync(Guid userId, CreateBookingRequest request, CancellationToken ct = default);
    Task<IEnumerable<BookingDto>> GetUserBookingsAsync(Guid userId, CancellationToken ct = default);
    Task<BookingDto> GetBookingAsync(Guid bookingId, Guid userId, CancellationToken ct = default);
    Task<bool> CancelBookingAsync(Guid bookingId, Guid userId, CancellationToken ct = default);
}

public interface IFlightService
{
    Task<IEnumerable<FlightDto>> SearchAsync(FlightSearchRequest request, CancellationToken ct = default);
    Task<BookingDto> BookAsync(Guid userId, CreateBookingRequest request, CancellationToken ct = default);
    Task<FlightAlertDto> CreateAlertAsync(Guid userId, CreateFlightAlertRequest request, CancellationToken ct = default);
    Task<IEnumerable<FlightAlertDto>> GetAlertsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> DeleteAlertAsync(Guid alertId, Guid userId, CancellationToken ct = default);
    Task<bool> ToggleAlertAsync(Guid alertId, Guid userId, CancellationToken ct = default);
    Task CheckAlertsAsync(CancellationToken ct = default); // Background job
}

public interface IAiAssistantService
{
    Task<AiChatResponse> ChatAsync(Guid userId, AiChatRequest request, CancellationToken ct = default);
    Task<string> SuggestItineraryAsync(Guid userId, AiSuggestRequest request, CancellationToken ct = default);
    Task<IEnumerable<AiChatResponse>> GetChatHistoryAsync(Guid itineraryId, Guid userId, CancellationToken ct = default);
    Task ClearChatHistoryAsync(Guid itineraryId, Guid userId, CancellationToken ct = default);
}

public interface IReportService
{
    Task<byte[]> GeneratePdfReportAsync(Guid userId, ReportRequest request, CancellationToken ct = default);
    Task<byte[]> GenerateCsvReportAsync(Guid userId, ReportRequest request, CancellationToken ct = default);
    Task<ExpenseReportDto> GetExpenseSummaryAsync(Guid itineraryId, Guid userId, CancellationToken ct = default);
}

public interface IEmailService
{
    Task SendPasswordResetAsync(string email, string name, string resetLink, string language, CancellationToken ct = default);
    Task SendFlightAlertAsync(string email, string name, string origin, string destination, decimal price, string language, CancellationToken ct = default);
    Task SendBookingConfirmationAsync(string email, string name, string bookingRef, string language, CancellationToken ct = default);
}

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}
