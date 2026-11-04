namespace TaskManager.Api.Dtos.Auth;

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt);
