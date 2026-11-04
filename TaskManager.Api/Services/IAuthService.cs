using TaskManager.Api.Dtos.Auth;

namespace TaskManager.Api.Services;

public interface IAuthService
{
    Task<UserResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
