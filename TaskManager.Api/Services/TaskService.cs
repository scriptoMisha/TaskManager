using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Dtos;
using TaskManager.Api.Dtos.Tasks;
using TaskManager.Api.Entities;

namespace TaskManager.Api.Services;

public class TaskService(AppDbContext db, TimeProvider timeProvider) : ITaskService
{
    public async Task<PagedResponse<TaskResponse>> GetListAsync(
        Guid userId, TaskQuery query, CancellationToken cancellationToken)
    {
        var tasks = db.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        tasks = query.Status switch
        {
            TaskStatusFilter.Done => tasks.Where(t => t.IsDone),
            TaskStatusFilter.Active => tasks.Where(t => !t.IsDone),
            _ => tasks
        };

        var totalCount = await tasks.CountAsync(cancellationToken);

        tasks = query.Sort switch
        {
            TaskSortOrder.Oldest => tasks.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id),
            TaskSortOrder.Title => tasks.OrderBy(t => t.Title).ThenBy(t => t.Id),
            _ => tasks.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
        };

        var items = await tasks
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => ToResponse(t))
            .ToListAsync(cancellationToken);

        return new PagedResponse<TaskResponse>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<TaskResponse?> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        return await db.Tasks
            .AsNoTracking()
            .Where(t => t.Id == taskId && t.UserId == userId)
            .Select(t => ToResponse(t))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TaskResponse> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            IsDone = false,
            CreatedAt = timeProvider.GetUtcNowTruncatedToMicroseconds(),
            UserId = userId
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(task);
    }

    public async Task<bool> UpdateAsync(
        Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await db.Tasks
            .SingleOrDefaultAsync(t => t.Id == taskId && t.UserId == userId, cancellationToken);

        if (task is null)
        {
            return false;
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.IsDone = request.IsDone;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> MarkDoneAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var updated = await db.Tasks
            .Where(t => t.Id == taskId && t.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDone, true), cancellationToken);

        return updated > 0;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var deleted = await db.Tasks
            .Where(t => t.Id == taskId && t.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    private static TaskResponse ToResponse(TaskItem task) =>
        new(task.Id, task.Title, task.Description, task.IsDone, task.CreatedAt);
}
