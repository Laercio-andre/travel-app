using MailKit.Net.Smtp;
using TravelSystem.Application.DTOs.Flight;
using TravelSystem.Application.DTOs.Hotel;
using TravelSystem.Domain.Entities;
using TravelSystem.Domain.Interfaces;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using TravelSystem.Application.Interfaces;
using TravelSystem.Domain.Enums;

namespace TravelSystem.Infrastructure.Services;

/// <summary>Email service using MailKit / SMTP.</summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string email, string name, string resetLink, string language, CancellationToken ct = default)
    {
        var subject = language.StartsWith("pt") ? "Redefinição de Senha — TravelSystem" : "Password Reset — TravelSystem";
        var body = language.StartsWith("pt")
            ? $"<p>Olá {name},</p><p>Clique <a href='{resetLink}'>aqui</a> para redefinir a sua senha. O link expira em 2 horas.</p>"
            : $"<p>Hello {name},</p><p>Click <a href='{resetLink}'>here</a> to reset your password. Link expires in 2 hours.</p>";

        await SendAsync(email, name, subject, body, ct);
    }

    public async Task SendFlightAlertAsync(string email, string name, string origin, string destination, decimal price, string language, CancellationToken ct = default)
    {
        var subject = language.StartsWith("pt")
            ? $"Alerta de Preço: {origin} → {destination}"
            : $"Price Alert: {origin} → {destination}";
        var body = language.StartsWith("pt")
            ? $"<p>Olá {name},</p><p>Um voo de <strong>{origin}</strong> para <strong>{destination}</strong> está disponível por <strong>{price:N2}</strong>.</p>"
            : $"<p>Hello {name},</p><p>A flight from <strong>{origin}</strong> to <strong>{destination}</strong> is available for <strong>{price:N2}</strong>.</p>";

        await SendAsync(email, name, subject, body, ct);
    }

    public async Task SendBookingConfirmationAsync(string email, string name, string bookingRef, string language, CancellationToken ct = default)
    {
        var subject = language.StartsWith("pt") ? $"Reserva Confirmada #{bookingRef}" : $"Booking Confirmed #{bookingRef}";
        var body = language.StartsWith("pt")
            ? $"<p>Olá {name},</p><p>A sua reserva <strong>#{bookingRef}</strong> foi confirmada com sucesso.</p>"
            : $"<p>Hello {name},</p><p>Your booking <strong>#{bookingRef}</strong> has been confirmed.</p>";

        await SendAsync(email, name, subject, body, ct);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct)
    {
        var host = _config["Email:Host"];
        var port = _config["Email:Port"];
        var username = _config["Email:Username"];
        var password = _config["Email:Password"];
        var fromAddress = _config["Email:FromAddress"];
        var fromName = _config["Email:FromName"] ?? "TravelSystem";

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(port) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromAddress) ||
            username.Contains("your-email", StringComparison.OrdinalIgnoreCase) ||
            password.Contains("your-app-password", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Email service is not configured. Subject '{Subject}' for {Email} was not sent.", subject, toEmail);
            throw new InvalidOperationException("EMAIL_NOT_CONFIGURED");
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, int.Parse(port), SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(username, password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw new InvalidOperationException("EMAIL_SEND_FAILED", ex);
        }
    }
}

/// <summary>Hotel service — wraps internal DB search + external API fallback.</summary>
public class HotelService : IHotelService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;
    private readonly ILogger<HotelService> _logger;

    public HotelService(IUnitOfWork uow, IEmailService emailService, ILogger<HotelService> logger)
    {
        _uow = uow;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IEnumerable<HotelDto>> SearchAsync(HotelSearchRequest request, CancellationToken ct = default)
    {
        var checkIn = request.CheckIn;
        var checkOut = request.CheckOut;
        var hotels = await _uow.Hotels.SearchAsync(request.City, checkIn, checkOut, request.Guests, ct);

        var dtos = hotels
            .Where(h => request.MinStars == null || h.StarRating >= request.MinStars)
            .Select(h => new HotelDto(
                h.Id, h.ExternalId, h.Provider, h.Name, h.Address, h.City, h.CountryCode,
                h.Latitude, h.Longitude, h.StarRating, h.GuestRating, h.ImageUrl, h.Description,
                h.Amenities,
                h.Rooms.Where(r => r.IsAvailable).MinBy(r => r.PricePerNight)?.PricePerNight,
                h.Rooms.FirstOrDefault()?.CurrencyCode
            ));

        if (request.MaxPrice.HasValue)
            dtos = dtos.Where(h => h.LowestPrice <= request.MaxPrice);

        return request.SortBy switch
        {
            "rating" => dtos.OrderByDescending(h => h.GuestRating),
            "stars" => dtos.OrderByDescending(h => h.StarRating),
            _ => dtos.OrderBy(h => h.LowestPrice)
        };
    }

    public async Task<HotelDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var hotel = await _uow.Hotels.GetWithRoomsAsync(id, ct)
            ?? throw new KeyNotFoundException("HOTEL_NOT_FOUND");

        return new HotelDetailDto(
            hotel.Id, hotel.Name, hotel.Address, hotel.Latitude, hotel.Longitude,
            hotel.StarRating, hotel.GuestRating, hotel.Description, hotel.Amenities,
            hotel.Rooms.Select(r => new HotelRoomDto(r.Id, r.RoomType, r.Description, r.MaxGuests, r.PricePerNight, r.CurrencyCode, r.IsAvailable)).ToList()
        );
    }

    public async Task<BookingDto> BookAsync(Guid userId, CreateBookingRequest request, CancellationToken ct = default)
    {
        var booking = new Booking
        {
            UserId = userId,
            HotelId = request.HotelId,
            HotelRoomId = request.HotelRoomId,
            Type = request.Type,
            CheckIn = request.CheckIn ?? DateTime.UtcNow.Date,
            CheckOut = request.CheckOut ?? DateTime.UtcNow.Date.AddDays(1),
            Guests = request.Guests,
            TotalPrice = request.TotalPrice > 0 ? request.TotalPrice : 1,
            CurrencyCode = request.CurrencyCode,
            Notes = request.Notes,
            Status = BookingStatus.Confirmed,
            ConfirmationNumber = GenerateConfirmationNumber()
        };

        await _uow.Bookings.AddAsync(booking, ct);
        await _uow.CommitAsync(ct);

        return MapToDto(booking);
    }

    public async Task<IEnumerable<BookingDto>> GetUserBookingsAsync(Guid userId, CancellationToken ct = default)
    {
        var bookings = await _uow.Bookings.GetByUserIdAsync(userId, ct);
        return bookings.Select(MapToDto);
    }

    public async Task<BookingDto> GetBookingAsync(Guid bookingId, Guid userId, CancellationToken ct = default)
    {
        var booking = await _uow.Bookings.GetWithDetailsAsync(bookingId, ct)
            ?? throw new KeyNotFoundException("BOOKING_NOT_FOUND");

        if (booking.UserId != userId) throw new UnauthorizedAccessException("ACCESS_DENIED");
        return MapToDto(booking);
    }

    public async Task<bool> CancelBookingAsync(Guid bookingId, Guid userId, CancellationToken ct = default)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId, ct)
            ?? throw new KeyNotFoundException("BOOKING_NOT_FOUND");

        if (booking.UserId != userId) throw new UnauthorizedAccessException("ACCESS_DENIED");
        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await _uow.CommitAsync(ct);
        return true;
    }

    private static string GenerateConfirmationNumber() =>
        $"TS{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(10000, 99999)}";

    private static BookingDto MapToDto(Booking b) =>
        new(b.Id, b.Type, b.Status, b.CheckIn, b.CheckOut, b.Guests,
            b.TotalPrice, b.CurrencyCode, b.ConfirmationNumber, b.CreatedAt,
            b.Hotel?.Name, b.Flight?.FlightNumber);
}

/// <summary>Flight service — local search + alert engine.</summary>
public class FlightService : IFlightService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;

    public FlightService(IUnitOfWork uow, IEmailService emailService)
    {
        _uow = uow;
        _emailService = emailService;
    }

    public async Task<IEnumerable<FlightDto>> SearchAsync(FlightSearchRequest request, CancellationToken ct = default)
    {
        var flights = await _uow.Flights.SearchAsync(
            request.EffectiveOrigin, request.EffectiveDestination, request.DepartureDate, request.Passengers, ct);

        var filtered = flights.Where(f => f.CabinClass == request.CabinClass);
        return (request.SortBy switch
        {
            "duration" => filtered.OrderBy(f => f.DurationMinutes),
            "departure" => filtered.OrderBy(f => f.DepartureAt),
            _ => filtered.OrderBy(f => f.Price)
        }).Select(MapToDto);
    }

    public async Task<BookingDto> BookAsync(Guid userId, CreateBookingRequest request, CancellationToken ct = default)
    {
        var booking = new Booking
        {
            UserId = userId,
            FlightId = request.FlightId,
            Type = request.FlightId.HasValue ? BookingType.Flight : request.Type,
            CheckIn = request.CheckIn ?? DateTime.UtcNow.Date,
            CheckOut = request.CheckOut ?? DateTime.UtcNow.Date,
            Guests = request.Guests,
            TotalPrice = request.TotalPrice > 0 ? request.TotalPrice : 1,
            CurrencyCode = request.CurrencyCode,
            Status = BookingStatus.Confirmed,
            ConfirmationNumber = $"FL{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(10000, 99999)}"
        };

        await _uow.Bookings.AddAsync(booking, ct);
        await _uow.CommitAsync(ct);
        return new BookingDto(booking.Id, booking.Type, booking.Status, booking.CheckIn, booking.CheckOut,
            booking.Guests, booking.TotalPrice, booking.CurrencyCode, booking.ConfirmationNumber, booking.CreatedAt, null, null);
    }

    public async Task<FlightAlertDto> CreateAlertAsync(Guid userId, CreateFlightAlertRequest request, CancellationToken ct = default)
    {
        var alert = new FlightAlert
        {
            UserId = userId,
            OriginCode = request.EffectiveOrigin,
            DestinationCode = request.EffectiveDestination,
            IsActive = request.Enabled,
            DepartureFrom = request.DepartureFrom,
            DepartureTo = request.DepartureTo,
            TargetPrice = request.TargetPrice,
            CurrencyCode = request.CurrencyCode
        };

        await _uow.FlightAlerts.AddAsync(alert, ct);
        await _uow.CommitAsync(ct);
        return MapAlertToDto(alert);
    }

    public async Task<IEnumerable<FlightAlertDto>> GetAlertsAsync(Guid userId, CancellationToken ct = default)
    {
        var alerts = await _uow.FlightAlerts.GetByUserIdAsync(userId, ct);
        return alerts.Select(MapAlertToDto);
    }

    public async Task<bool> DeleteAlertAsync(Guid alertId, Guid userId, CancellationToken ct = default)
    {
        var alert = await _uow.FlightAlerts.GetByIdAsync(alertId, ct)
            ?? throw new KeyNotFoundException("ALERT_NOT_FOUND");
        if (alert.UserId != userId) throw new UnauthorizedAccessException("ACCESS_DENIED");
        await _uow.FlightAlerts.DeleteAsync(alert, ct);
        await _uow.CommitAsync(ct);
        return true;
    }

    public async Task<bool> ToggleAlertAsync(Guid alertId, Guid userId, CancellationToken ct = default)
    {
        var alert = await _uow.FlightAlerts.GetByIdAsync(alertId, ct)
            ?? throw new KeyNotFoundException("ALERT_NOT_FOUND");
        if (alert.UserId != userId) throw new UnauthorizedAccessException("ACCESS_DENIED");
        alert.IsActive = !alert.IsActive;
        await _uow.CommitAsync(ct);
        return alert.IsActive;
    }

    /// <summary>Run by a background job — checks active alerts against available flights.</summary>
    public async Task CheckAlertsAsync(CancellationToken ct = default)
    {
        var activeAlerts = await _uow.FlightAlerts.GetActiveAlertsAsync(ct);

        foreach (var alert in activeAlerts)
        {
            var flights = await _uow.Flights.SearchAsync(
                alert.OriginCode, alert.DestinationCode,
                alert.DepartureFrom ?? DateTime.UtcNow.Date, 1, ct);

            var cheapest = flights.Where(f => f.Price <= alert.TargetPrice).MinBy(f => f.Price);

            if (cheapest is not null)
            {
                await _emailService.SendFlightAlertAsync(
                    alert.User.Email!, alert.User.FirstName,
                    alert.OriginCode, alert.DestinationCode,
                    cheapest.Price, alert.User.PreferredLanguage, ct);

                alert.LastTriggeredAt = DateTime.UtcNow;
                await _uow.CommitAsync(ct);
            }
        }
    }

    private static FlightDto MapToDto(Flight f) =>
        new(f.Id, f.ExternalId, f.Provider, f.Airline, f.FlightNumber,
            f.OriginCode, f.DestinationCode, f.OriginCity, f.DestinationCity,
            f.DepartureAt, f.ArrivalAt, f.DurationMinutes, f.Stops,
            f.CabinClass, f.Price, f.CurrencyCode, f.SeatsAvailable);

    private static FlightAlertDto MapAlertToDto(FlightAlert a) =>
        new(a.Id, a.OriginCode, a.DestinationCode, a.DepartureFrom, a.DepartureTo,
            a.TargetPrice, a.CurrencyCode, a.IsActive, a.LastTriggeredAt, a.CreatedAt);
}
