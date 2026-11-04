using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Api.Dtos.Auth;
using TaskManager.Api.Entities;

namespace TaskManager.Api.Auth;

public class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AuthResponse CreateToken(User user)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(jwt.ExpiresMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        };

        var token = _tokenHandler.CreateToken(descriptor);

        return new AuthResponse(token, expiresAt);
    }
}
