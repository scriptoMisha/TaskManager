using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Api.Auth;
using TaskManager.Api.Entities;

namespace TaskManager.Tests.Auth;

public class TokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        Key = "unit-tests-signing-key-at-least-32-bytes-long",
        ExpiresMinutes = 15
    };

    private static readonly User User = new()
    {
        Id = Guid.NewGuid(),
        Email = "alice@example.com"
    };

    [Fact]
    public void CreateToken_ContainsUserClaimsAndExpiry()
    {
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var service = CreateService(now);

        var result = service.CreateToken(User);

        var token = new JsonWebToken(result.AccessToken);
        Assert.Equal(User.Id.ToString(), token.Subject);
        Assert.Equal(User.Email, token.GetClaim(JwtRegisteredClaimNames.Email).Value);
        Assert.False(string.IsNullOrEmpty(token.Id));
        Assert.Equal(Options.Issuer, token.Issuer);
        Assert.Equal(Options.Audience, Assert.Single(token.Audiences));
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Alg);
        Assert.Equal(now.UtcDateTime.AddMinutes(15), token.ValidTo);
        Assert.Equal(token.ValidTo, result.ExpiresAt);
    }

    [Fact]
    public void CreateToken_DoesNotContainPasswordHash()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "bob@example.com", PasswordHash = "$2a$11$secret-hash" };

        var result = CreateService(DateTimeOffset.UtcNow).CreateToken(user);

        var payload = Base64UrlEncoder.Decode(new JsonWebToken(result.AccessToken).EncodedPayload);
        Assert.DoesNotContain(user.PasswordHash, payload);
    }

    [Fact]
    public async Task CreateToken_IsValidWithSameKey()
    {
        var result = CreateService(DateTimeOffset.UtcNow).CreateToken(User);

        var validation = await ValidateAsync(result.AccessToken, Options.Key);

        Assert.True(validation.IsValid);
    }

    [Fact]
    public async Task CreateToken_IsRejectedWithDifferentKey()
    {
        var result = CreateService(DateTimeOffset.UtcNow).CreateToken(User);

        var validation = await ValidateAsync(result.AccessToken, "another-signing-key-that-is-32-bytes-or-more");

        Assert.False(validation.IsValid);
        Assert.IsType<SecurityTokenSignatureKeyNotFoundException>(validation.Exception);
    }

    [Fact]
    public async Task CreateToken_IsRejectedAfterExpiry()
    {
        var result = CreateService(DateTimeOffset.UtcNow.AddHours(-1)).CreateToken(User);

        var validation = await ValidateAsync(result.AccessToken, Options.Key);

        Assert.False(validation.IsValid);
        Assert.IsType<SecurityTokenExpiredException>(validation.Exception);
    }

    private static TokenService CreateService(DateTimeOffset now) =>
        new(Microsoft.Extensions.Options.Options.Create(Options), new FixedTimeProvider(now));

    private static Task<TokenValidationResult> ValidateAsync(string token, string key) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.Zero
        });

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
