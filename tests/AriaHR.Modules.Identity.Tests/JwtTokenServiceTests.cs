using System.IdentityModel.Tokens.Jwt;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace AriaHR.Modules.Identity.Tests;

public class JwtTokenServiceTests
{
    private readonly IOptions<JwtOptions> _jwtOptions;

    public JwtTokenServiceTests()
    {
        _jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "AriaHR.TestIssuer",
            Audience = "AriaHR.TestAudience",
            SecretKey = "SUPER_SECRET_KEY_FOR_UNIT_TESTING_PURPOSES_ONLY_MIN_256_BITS",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        });
    }

    [Fact]
    public void GenerateAccessToken_UserWithOrganizationId_IncludesOrganizationIdClaim()
    {
        // Arrange
        var tokenService = new JwtTokenService(_jwtOptions);
        var orgId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Center",
            LastName = "Manager",
            PhoneNumber = "09120001122",
            OrganizationId = orgId,
            IsActive = true
        };
        var roles = new[] { "CenterManager" };

        // Act
        var tokenString = tokenService.GenerateAccessToken(user, roles);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(tokenString));
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        var orgClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "organization_id");
        Assert.NotNull(orgClaim);
        Assert.Equal(orgId.ToString(), orgClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_UserWithoutOrganizationId_OmitsOrganizationIdClaim()
    {
        // Arrange
        var tokenService = new JwtTokenService(_jwtOptions);
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = "09120001133",
            OrganizationId = null,
            IsActive = true
        };
        var roles = new[] { "SystemAdmin" };

        // Act
        var tokenString = tokenService.GenerateAccessToken(user, roles);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(tokenString));
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        var orgClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "organization_id");
        Assert.Null(orgClaim);
    }
}
