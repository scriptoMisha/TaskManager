using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.Dtos.Tasks;

public sealed record UpdateTaskRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public bool IsDone { get; init; }
}
