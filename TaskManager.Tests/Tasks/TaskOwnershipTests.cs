using System.Net;
using System.Net.Http.Json;
using TaskManager.Api.Dtos;
using TaskManager.Api.Dtos.Tasks;
using TaskManager.Tests.Infrastructure;

namespace TaskManager.Tests.Tasks;

[Collection(ApiCollection.Name)]
public class TaskOwnershipTests(ApiFactory factory)
{
    [Fact]
    public async Task GetList_ReturnsOnlyOwnTasks()
    {
        var alice = await factory.CreateAuthorizedClientAsync();
        var bob = await factory.CreateAuthorizedClientAsync();
        await alice.CreateTaskAsync("Alice task");
        await bob.CreateTaskAsync("Bob task");

        var page = await bob.GetFromJsonAsync<PagedResponse<TaskResponse>>("/api/tasks");

        var task = Assert.Single(page!.Items);
        Assert.Equal("Bob task", task.Title);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task GetById_OtherUsersTask_Returns404()
    {
        var (aliceTask, _, bob) = await CreateAliceTaskAndBobAsync();

        var response = await bob.GetAsync($"/api/tasks/{aliceTask.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_OtherUsersTask_Returns404AndKeepsTask()
    {
        var (aliceTask, alice, bob) = await CreateAliceTaskAndBobAsync();

        var response = await bob.PutAsJsonAsync(
            $"/api/tasks/{aliceTask.Id}",
            new { title = "Hacked", isDone = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(aliceTask, await alice.GetFromJsonAsync<TaskResponse>($"/api/tasks/{aliceTask.Id}"));
    }

    [Fact]
    public async Task MarkDone_OtherUsersTask_Returns404AndKeepsTask()
    {
        var (aliceTask, alice, bob) = await CreateAliceTaskAndBobAsync();

        var response = await bob.PatchAsync($"/api/tasks/{aliceTask.Id}/done", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(aliceTask, await alice.GetFromJsonAsync<TaskResponse>($"/api/tasks/{aliceTask.Id}"));
    }

    [Fact]
    public async Task Delete_OtherUsersTask_Returns404AndKeepsTask()
    {
        var (aliceTask, alice, bob) = await CreateAliceTaskAndBobAsync();

        var response = await bob.DeleteAsync($"/api/tasks/{aliceTask.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(aliceTask, await alice.GetFromJsonAsync<TaskResponse>($"/api/tasks/{aliceTask.Id}"));
    }

    private async Task<(TaskResponse AliceTask, HttpClient Alice, HttpClient Bob)> CreateAliceTaskAndBobAsync()
    {
        var alice = await factory.CreateAuthorizedClientAsync();
        var bob = await factory.CreateAuthorizedClientAsync();
        var aliceTask = await alice.CreateTaskAsync("Alice private task", "secret");

        return (aliceTask, alice, bob);
    }
}
