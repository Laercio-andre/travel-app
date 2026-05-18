namespace TravelSystem.Application.DTOs.Auth;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? ConfirmPassword = null,
    string PreferredLanguage = "pt-AO"
);

public record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false
);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword,
    string? ConfirmPassword = null
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);

public record RefreshTokenRequest(string RefreshToken);

public record AuthResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);

public record UserProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string PreferredLanguage,
    string? AvatarUrl,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    bool IsActive
)
{
    public Guid UserId => Id;
}

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string PreferredLanguage,
    string? AvatarUrl
);
