using TaskManager.Api.Dtos;
using TaskManager.Api.Dtos.Tasks;

namespace TaskManager.Api.Services;

public interface ITaskService
{
    Task<PagedResponse<TaskResponse>> GetListAsync(Guid userId, TaskQuery query, CancellationToken cancellationToken);

    Task<TaskResponse?> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);

    Task<TaskResponse> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken);

    Task<bool> MarkDoneAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);
}
