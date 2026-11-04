using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Auth;
using TaskManager.Api.Data;
using TaskManager.Api.Dtos.Auth;
using TaskManager.Api.Entities;

namespace TaskManager.Api.Services;

public class AuthService(AppDbContext db, ITokenService tokenService, TimeProvider timeProvider) : IAuthService
{
    public async Task<UserResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password),
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return new UserResponse(user.Id, user.Email, user.CreatedAt);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        var user = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return tokenService.CreateToken(user);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
