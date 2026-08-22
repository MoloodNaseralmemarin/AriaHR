namespace AriaHR.Modules.Identity.Application.DTOs;

public record AuthenticationResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiration,
    DateTime RefreshTokenExpiration);
