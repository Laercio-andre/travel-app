using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TravelSystem.Application.DTOs.Auth;
using TravelSystem.Application.Interfaces;
using TravelSystem.Domain.Entities;

namespace TravelSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration config,
        IEmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _config = config;
        _emailService = emailService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            throw new InvalidOperationException("EMAIL_ALREADY_EXISTS");

        if (!string.IsNullOrWhiteSpace(request.ConfirmPassword) && request.Password != request.ConfirmPassword)
            throw new ArgumentException("PASSWORDS_DO_NOT_MATCH");

        var user = new User
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PreferredLanguage = request.PreferredLanguage,
            EmailConfirmed = true // In production: send confirmation email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        await EnsureRoleExistsAsync("Traveler");
        await _userManager.AddToRoleAsync(user, "Traveler");

        return await BuildAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("INVALID_CREDENTIALS");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("ACCOUNT_DEACTIVATED");

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("INVALID_CREDENTIALS");

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await BuildAuthResponseAsync(user, request.RememberMe);
    }

    public async Task LogoutAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("USER_NOT_FOUND");

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _userManager.UpdateAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var user = _userManager.Users
            .Where(u => u.RefreshToken == request.RefreshToken && u.RefreshTokenExpiry > DateTime.UtcNow)
            .FirstOrDefault()
            ?? throw new UnauthorizedAccessException("INVALID_REFRESH_TOKEN");

        return await BuildAuthResponseAsync(user);
    }

    public async Task<string?> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) return null; // Don't reveal if email exists

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(2);
        await _userManager.UpdateAsync(user);

        var resetLink = $"{_config["Frontend:Url"]}/auth/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendPasswordResetAsync(request.Email, user.FirstName, resetLink, user.PreferredLanguage, ct);
        return token;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.ConfirmPassword) && request.NewPassword != request.ConfirmPassword)
            throw new ArgumentException("PASSWORDS_DO_NOT_MATCH");

        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new KeyNotFoundException("USER_NOT_FOUND");

        if (user.PasswordResetToken != request.Token || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("INVALID_OR_EXPIRED_TOKEN");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.RefreshToken = null;
        await _userManager.UpdateAsync(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new ArgumentException("PASSWORDS_DO_NOT_MATCH");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("USER_NOT_FOUND");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        user.RefreshToken = null;
        await _userManager.UpdateAsync(user);
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("USER_NOT_FOUND");

        var roles = await _userManager.GetRolesAsync(user);
        return MapToProfile(user, roles.FirstOrDefault() ?? "Traveler");
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("USER_NOT_FOUND");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PreferredLanguage = request.PreferredLanguage;
        user.AvatarUrl = request.AvatarUrl;

        await _userManager.UpdateAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        return MapToProfile(user, roles.FirstOrDefault() ?? "Traveler");
    }

    public async Task<IEnumerable<UserProfileDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = _userManager.Users.ToList();
        var result = new List<UserProfileDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(MapToProfile(user, roles.FirstOrDefault() ?? "Traveler"));
        }

        return result;
    }

    public async Task<bool> DeactivateUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("USER_NOT_FOUND");

        user.IsActive = false;
        await _userManager.UpdateAsync(user);
        return true;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<AuthResponse> BuildAuthResponseAsync(User user, bool rememberMe = false)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Traveler";

        var accessToken = GenerateAccessToken(user, role);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

        return new AuthResponse(
            user.Id, user.Email!, user.FirstName, user.LastName, role,
            accessToken, refreshToken,
            DateTime.UtcNow.AddMinutes(expiryMinutes)
        );
    }

    private string GenerateAccessToken(User user, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim(ClaimTypes.Role, role),
            new Claim("lang", user.PreferredLanguage),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static UserProfileDto MapToProfile(User user, string role) =>
        new(user.Id, user.Email!, user.FirstName, user.LastName,
            user.PreferredLanguage, user.AvatarUrl, role, user.CreatedAt, user.LastLoginAt, user.IsActive);

    private async Task EnsureRoleExistsAsync(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }
}

// Extension to avoid EF query issues with UserManager
internal static class UserManagerExtensions
{
    internal static IQueryable<User> Where(this IEnumerable<User> source, Func<User, bool> predicate)
        => source.AsQueryable().Where(predicate);
}
