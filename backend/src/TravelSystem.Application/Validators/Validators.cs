using FluentValidation;
using TravelSystem.Application.DTOs.Auth;
using TravelSystem.Application.DTOs.Itinerary;
using TravelSystem.Application.DTOs.Hotel;
using TravelSystem.Application.DTOs.Flight;
using TravelSystem.Application.DTOs.AI;
using TravelSystem.Domain.Enums;

namespace TravelSystem.Application.Validators;

// ── Auth ─────────────────────────────────────────────────────────────────────

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .When(x => !string.IsNullOrWhiteSpace(x.ConfirmPassword))
            .WithMessage("Passwords do not match.");
        RuleFor(x => x.PreferredLanguage).Must(l => new[] { "pt", "pt-AO", "en" }.Contains(l))
            .WithMessage("Unsupported language. Use 'pt', 'pt-AO' or 'en'.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .When(x => !string.IsNullOrWhiteSpace(x.ConfirmPassword))
            .WithMessage("Passwords do not match.");
    }
}

// ── Itinerary ─────────────────────────────────────────────────────────────────

public class CreateItineraryRequestValidator : AbstractValidator<CreateItineraryRequest>
{
    public CreateItineraryRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Destination).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Start date cannot be in the past.");
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be after start date.");
        RuleFor(x => x.Budget).GreaterThan(0).When(x => x.Budget.HasValue);
        RuleFor(x => x.CountryCode).MaximumLength(3).When(x => x.CountryCode != null);
        When(x => x.Latitude.HasValue || x.Longitude.HasValue, () =>
        {
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        });
    }
}

public class CreateStopRequestValidator : AbstractValidator<CreateStopRequest>
{
    public CreateStopRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.DayNumber).GreaterThan(0);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x => x.DurationMinutes.HasValue);
    }
}

// ── Hotel ─────────────────────────────────────────────────────────────────────

public class HotelSearchRequestValidator : AbstractValidator<HotelSearchRequest>
{
    public HotelSearchRequestValidator()
    {
        RuleFor(x => x.City).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CheckIn).GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Check-in cannot be in the past.");
        RuleFor(x => x.CheckOut).GreaterThan(x => x.CheckIn)
            .WithMessage("Check-out must be after check-in.");
        RuleFor(x => x.Guests).InclusiveBetween(1, 20);
        RuleFor(x => x.MinStars).InclusiveBetween(1, 5).When(x => x.MinStars.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThan(0).When(x => x.MaxPrice.HasValue);
    }
}

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.CheckIn).LessThan(x => x.CheckOut).When(x => x.CheckIn.HasValue && x.CheckOut.HasValue);
        RuleFor(x => x.Guests).InclusiveBetween(1, 20);
        RuleFor(x => x.TotalPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.HotelId).NotNull()
            .When(x => x.Type == BookingType.Hotel)
            .WithMessage("HotelId is required for hotel bookings.");
        RuleFor(x => x.FlightId).NotNull()
            .When(x => x.Type == BookingType.Flight)
            .WithMessage("FlightId is required for flight bookings.");
    }
}

// ── Flight ────────────────────────────────────────────────────────────────────

public class FlightSearchRequestValidator : AbstractValidator<FlightSearchRequest>
{
    private static readonly string[] ValidCabins = ["economy", "premium_economy", "business", "first"];

    public FlightSearchRequestValidator()
    {
        RuleFor(x => x.EffectiveOrigin).NotEmpty().Length(3)
            .Matches("^[A-Z]{3}$").WithMessage("Origin must be a valid 3-letter IATA code (e.g. LAD).");
        RuleFor(x => x.EffectiveDestination).NotEmpty().Length(3)
            .Matches("^[A-Z]{3}$").WithMessage("Destination must be a valid 3-letter IATA code.");
        RuleFor(x => x.DepartureDate).GreaterThanOrEqualTo(DateTime.UtcNow.Date);
        RuleFor(x => x.Passengers).InclusiveBetween(1, 9);
        RuleFor(x => x.CabinClass).Must(c => ValidCabins.Contains(c))
            .WithMessage("Invalid cabin class. Use: economy, premium_economy, business, first.");
    }
}

public class CreateFlightAlertRequestValidator : AbstractValidator<CreateFlightAlertRequest>
{
    public CreateFlightAlertRequestValidator()
    {
        RuleFor(x => x.EffectiveOrigin).NotEmpty().Length(3).Matches("^[A-Z]{3}$");
        RuleFor(x => x.EffectiveDestination).NotEmpty().Length(3).Matches("^[A-Z]{3}$");
        RuleFor(x => x.TargetPrice).GreaterThan(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.DepartureTo).GreaterThan(x => x.DepartureFrom)
            .When(x => x.DepartureFrom.HasValue && x.DepartureTo.HasValue);
    }
}

// ── AI ────────────────────────────────────────────────────────────────────────

public class AiChatRequestValidator : AbstractValidator<AiChatRequest>
{
    public AiChatRequestValidator()
    {
        RuleFor(x => x.ItineraryId).NotEmpty();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Language).Must(l => new[] { "pt", "en" }.Contains(l))
            .WithMessage("Language must be 'pt' or 'en'.");
    }
}

public class AiSuggestRequestValidator : AbstractValidator<AiSuggestRequest>
{
    public AiSuggestRequestValidator()
    {
        RuleFor(x => x.Destination).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DurationDays).InclusiveBetween(1, 30);
        RuleFor(x => x.Budget).GreaterThan(0).When(x => x.Budget.HasValue);
    }
}
