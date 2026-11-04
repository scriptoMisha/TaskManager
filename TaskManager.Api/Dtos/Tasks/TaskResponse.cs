namespace TaskManager.Api.Dtos.Tasks;

public sealed record TaskResponse(Guid Id, string Title, string? Description, bool IsDone, DateTime CreatedAt);
