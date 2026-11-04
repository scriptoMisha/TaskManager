namespace TaskManager.Api.Dtos.Auth;

public sealed record UserResponse(Guid Id, string Email, DateTime CreatedAt);
