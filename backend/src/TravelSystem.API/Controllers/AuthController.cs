using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelSystem.Application.DTOs.Auth;
using TravelSystem.Application.Interfaces;

namespace TravelSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthService auth,
        ICurrentUserService currentUser,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _auth = auth;
        _currentUser = currentUser;
        _environment = environment;
        _configuration = configuration;
    }

    /// <summary>Registar novo utilizador</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, ct);
        return Created($"/api/auth/profile", result);
    }

    /// <summary>Autenticar utilizador</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await _auth.LoginAsync(request, ct));

    /// <summary>Terminar sessão</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _auth.LogoutAsync(_currentUser.UserId, ct);
        return NoContent();
    }

    /// <summary>Renovar token de acesso</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
        => Ok(await _auth.RefreshTokenAsync(request, ct));

    /// <summary>Solicitar recuperação de senha</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var token = await _auth.ForgotPasswordAsync(request, ct);
        var frontendUrl = _configuration["Frontend:Url"]?.TrimEnd('/') ?? "http://localhost:4200";
        var resetUrl = token is null
            ? null
            : $"{frontendUrl}/auth/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";

        return Ok(new ForgotPasswordResponse(
            "PASSWORD_RESET_EMAIL_SENT",
            _environment.IsDevelopment() ? token : null,
            _environment.IsDevelopment() ? resetUrl : null
        ));
    }

    /// <summary>Redefinir senha com token</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _auth.ResetPasswordAsync(request, ct);
        return Ok(new { message = "PASSWORD_RESET_SUCCESS" });
    }

    /// <summary>Alterar senha</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await _auth.ChangePasswordAsync(_currentUser.UserId, request, ct);
        return Ok(new { message = "PASSWORD_CHANGED" });
    }

    /// <summary>Obter perfil do utilizador autenticado</summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken ct)
        => Ok(await _auth.GetProfileAsync(_currentUser.UserId, ct));

    /// <summary>Atualizar perfil</summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
        => Ok(await _auth.UpdateProfileAsync(_currentUser.UserId, request, ct));
}
