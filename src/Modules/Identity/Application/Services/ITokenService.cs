using AriaHR.Modules.Identity.Domain.Entities;

namespace AriaHR.Modules.Identity.Application.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    string HashToken(string token);
    int AccessTokenExpirationMinutes { get; }
    int RefreshTokenExpirationDays { get; }
}
