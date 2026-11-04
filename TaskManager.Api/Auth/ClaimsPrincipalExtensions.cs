using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TaskManager.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated user has no valid 'sub' claim.");
    }
}
