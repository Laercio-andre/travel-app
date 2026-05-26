using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelSystem.Application.DTOs.AI;
using TravelSystem.Application.DTOs.Auth;
using TravelSystem.Application.DTOs.Flight;
using TravelSystem.Application.DTOs.Hotel;
using TravelSystem.Application.DTOs.Itinerary;
using TravelSystem.Application.DTOs.Report;
using TravelSystem.Application.Interfaces;

namespace TravelSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ItinerariesController : ControllerBase
{
    private readonly IItineraryService _service;
    private readonly ICurrentUserService _currentUser;

    public ItinerariesController(IItineraryService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItineraryDto>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetByUserAsync(_currentUser.UserId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ItineraryDetailDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, _currentUser.UserId, ct));

    [HttpPost]
    public async Task<ActionResult<ItineraryDto>> Create([FromBody] CreateItineraryRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(_currentUser.UserId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ItineraryDto>> Update(Guid id, [FromBody] UpdateItineraryRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, _currentUser.UserId, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, _currentUser.UserId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/stops")]
    public async Task<ActionResult<ItineraryStopDto>> AddStop(Guid id, [FromBody] CreateStopRequest request, CancellationToken ct)
        => Ok(await _service.AddStopAsync(id, _currentUser.UserId, request, ct));

    [HttpDelete("{id:guid}/stops/{stopId:guid}")]
    public async Task<IActionResult> DeleteStop(Guid id, Guid stopId, CancellationToken ct)
    {
        await _service.DeleteStopAsync(id, stopId, _currentUser.UserId, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/stops/{stopId:guid}/reorder")]
    public async Task<ActionResult<ItineraryStopDto>> ReorderStop(Guid id, Guid stopId, [FromBody] int newOrder, CancellationToken ct)
        => Ok(await _service.ReorderStopAsync(id, stopId, _currentUser.UserId, newOrder, ct));
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class HotelsController : ControllerBase
{
    private readonly IHotelService _service;
    private readonly ICurrentUserService _currentUser;

    public HotelsController(IHotelService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<HotelDto>>> Search([FromQuery] HotelSearchRequest request, CancellationToken ct)
        => Ok(await _service.SearchAsync(request, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HotelDetailDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost("bookings")]
    public async Task<ActionResult<BookingDto>> Book([FromBody] CreateBookingRequest request, CancellationToken ct)
        => Ok(await _service.BookAsync(_currentUser.UserId, request, ct));

    [HttpGet("bookings")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetBookings(CancellationToken ct)
        => Ok(await _service.GetUserBookingsAsync(_currentUser.UserId, ct));

    [HttpGet("bookings/{bookingId:guid}")]
    public async Task<ActionResult<BookingDto>> GetBooking(Guid bookingId, CancellationToken ct)
        => Ok(await _service.GetBookingAsync(bookingId, _currentUser.UserId, ct));

    [HttpDelete("bookings/{bookingId:guid}")]
    public async Task<IActionResult> CancelBooking(Guid bookingId, CancellationToken ct)
    {
        await _service.CancelBookingAsync(bookingId, _currentUser.UserId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class FlightsController : ControllerBase
{
    private readonly IFlightService _service;
    private readonly ICurrentUserService _currentUser;

    public FlightsController(IFlightService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<FlightDto>>> Search([FromQuery] FlightSearchRequest request, CancellationToken ct)
        => Ok(await _service.SearchAsync(request, ct));

    [HttpPost("bookings")]
    public async Task<ActionResult<BookingDto>> Book([FromBody] CreateBookingRequest request, CancellationToken ct)
        => Ok(await _service.BookAsync(_currentUser.UserId, request, ct));

    [HttpGet("alerts")]
    public async Task<ActionResult<IEnumerable<FlightAlertDto>>> GetAlerts(CancellationToken ct)
        => Ok(await _service.GetAlertsAsync(_currentUser.UserId, ct));

    [HttpPost("alerts")]
    public async Task<ActionResult<FlightAlertDto>> CreateAlert([FromBody] CreateFlightAlertRequest request, CancellationToken ct)
        => Ok(await _service.CreateAlertAsync(_currentUser.UserId, request, ct));

    [HttpDelete("alerts/{alertId:guid}")]
    public async Task<IActionResult> DeleteAlert(Guid alertId, CancellationToken ct)
    {
        await _service.DeleteAlertAsync(alertId, _currentUser.UserId, ct);
        return NoContent();
    }

    [HttpPatch("alerts/{alertId:guid}/toggle")]
    public async Task<IActionResult> ToggleAlert(Guid alertId, CancellationToken ct)
    {
        await _service.ToggleAlertAsync(alertId, _currentUser.UserId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/ai")]
[Authorize]
[Produces("application/json")]
public class AiController : ControllerBase
{
    private readonly IAiAssistantService _service;
    private readonly ICurrentUserService _currentUser;

    public AiController(IAiAssistantService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<AiChatResponse>> Chat([FromBody] AiChatRequest request, CancellationToken ct)
        => Ok(await _service.ChatAsync(_currentUser.UserId, request, ct));

    [HttpPost("suggest")]
    public async Task<ActionResult<string>> Suggest([FromBody] AiSuggestRequest request, CancellationToken ct)
        => Ok(await _service.SuggestItineraryAsync(_currentUser.UserId, request, ct));

    [HttpGet("chat/{itineraryId:guid}")]
    public async Task<ActionResult<IEnumerable<AiChatResponse>>> GetHistory(Guid itineraryId, CancellationToken ct)
        => Ok(await _service.GetChatHistoryAsync(itineraryId, _currentUser.UserId, ct));

    [HttpDelete("chat/{itineraryId:guid}")]
    public async Task<IActionResult> ClearHistory(Guid itineraryId, CancellationToken ct)
    {
        await _service.ClearChatHistoryAsync(itineraryId, _currentUser.UserId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _service;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(IReportService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet("summary/{itineraryId:guid}")]
    public async Task<ActionResult<ExpenseReportDto>> GetSummary(Guid itineraryId, CancellationToken ct)
        => Ok(await _service.GetExpenseSummaryAsync(itineraryId, _currentUser.UserId, ct));

    [HttpPost("pdf")]
    public async Task<IActionResult> DownloadPdf([FromBody] ReportRequest request, CancellationToken ct)
    {
        var bytes = await _service.GeneratePdfReportAsync(_currentUser.UserId, request, ct);
        return File(bytes, "application/pdf", $"relatorio-viagem-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    [HttpPost("csv")]
    public async Task<IActionResult> DownloadCsv([FromBody] ReportRequest request, CancellationToken ct)
    {
        var bytes = await _service.GenerateCsvReportAsync(_currentUser.UserId, request, ct);
        return File(bytes, "text/csv", $"despesas-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;
    private readonly IWebHostEnvironment _environment;

    public AdminController(IAuthService auth, ICurrentUserService currentUser, IWebHostEnvironment environment)
    {
        _auth = auth;
        _currentUser = currentUser;
        _environment = environment;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetAllUsers(CancellationToken ct)
        => Ok(await _auth.GetAllUsersAsync(ct));

    [HttpPatch("users/{userId:guid}/deactivate")]
    public async Task<ActionResult<UserProfileDto>> DeactivateUser(Guid userId, CancellationToken ct)
    {
        if (userId == _currentUser.UserId)
            return BadRequest(new { error = "CANNOT_DEACTIVATE_SELF" });

        return Ok(await _auth.DeactivateUserAsync(userId, ct));
    }

    [HttpPatch("users/{userId:guid}/activate")]
    public async Task<ActionResult<UserProfileDto>> ActivateUser(Guid userId, CancellationToken ct)
        => Ok(await _auth.ActivateUserAsync(userId, ct));

    [HttpPatch("users/{userId:guid}/role")]
    public async Task<ActionResult<UserProfileDto>> SetUserRole(Guid userId, [FromBody] SetUserRoleRequest request, CancellationToken ct)
    {
        if (userId == _currentUser.UserId)
            return BadRequest(new { error = "CANNOT_CHANGE_OWN_ROLE" });

        return Ok(await _auth.SetUserRoleAsync(userId, request, ct));
    }

    [HttpPost("users/{userId:guid}/password-reset")]
    public async Task<ActionResult<ForgotPasswordResponse>> SendPasswordReset(Guid userId, CancellationToken ct)
    {
        var token = await _auth.SendPasswordResetAsync(userId, ct);
        return Ok(new ForgotPasswordResponse(
            "PASSWORD_RESET_EMAIL_SENT",
            _environment.IsDevelopment() ? token : null
        ));
    }
}
