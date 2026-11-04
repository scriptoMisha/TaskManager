using TaskManager.Api.Dtos.Auth;
using TaskManager.Api.Entities;

namespace TaskManager.Api.Auth;

public interface ITokenService
{
    AuthResponse CreateToken(User user);
}
