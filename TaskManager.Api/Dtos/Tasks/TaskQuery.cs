using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.Dtos.Tasks;

public sealed record TaskQuery
{
    public TaskStatusFilter? Status { get; init; }

    public TaskSortOrder Sort { get; init; } = TaskSortOrder.Newest;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
