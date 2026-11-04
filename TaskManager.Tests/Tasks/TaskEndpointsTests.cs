using System.Net;
using System.Net.Http.Json;
using TaskManager.Api.Dtos;
using TaskManager.Api.Dtos.Tasks;
using TaskManager.Tests.Infrastructure;

namespace TaskManager.Tests.Tasks;

[Collection(ApiCollection.Name)]
public class TaskEndpointsTests(ApiFactory factory)
{
    [Theory]
    [InlineData("GET", "/api/tasks")]
    [InlineData("GET", "/api/tasks/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/tasks")]
    [InlineData("PUT", "/api/tasks/00000000-0000-0000-0000-000000000001")]
    [InlineData("PATCH", "/api/tasks/00000000-0000-0000-0000-000000000001/done")]
    [InlineData("DELETE", "/api/tasks/00000000-0000-0000-0000-000000000001")]
    public async Task Endpoints_WithoutToken_Return401(string method, string url)
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreatedTaskWithLocation()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tasks", new { title = "Buy milk", description = "2 liters" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.NotNull(task);
        Assert.Equal("Buy milk", task.Title);
        Assert.Equal("2 liters", task.Description);
        Assert.False(task.IsDone);
        Assert.Equal($"/api/tasks/{task.Id}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Create_WithEmptyTitle_Returns400()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tasks", new { title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsTask()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var created = await client.CreateTaskAsync("Read a book");

        var response = await client.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.Equal(created, task);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReplacesFields()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var created = await client.CreateTaskAsync("Old title", "old");

        var response = await client.PutAsJsonAsync(
            $"/api/tasks/{created.Id}",
            new { title = "New title", description = (string?)null, isDone = true });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var task = await client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{created.Id}");
        Assert.Equal("New title", task!.Title);
        Assert.Null(task.Description);
        Assert.True(task.IsDone);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var response = await client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", new { title = "x", isDone = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkDone_SetsIsDone()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var created = await client.CreateTaskAsync("Walk the dog");

        var response = await client.PatchAsync($"/api/tasks/{created.Id}/done", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var task = await client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{created.Id}");
        Assert.True(task!.IsDone);
    }

    [Fact]
    public async Task MarkDone_UnknownId_Returns404()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var response = await client.PatchAsync($"/api/tasks/{Guid.NewGuid()}/done", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTask()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var created = await client.CreateTaskAsync("Temporary");

        var response = await client.DeleteAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await client.GetAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var response = await client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("done", new[] { "B" })]
    [InlineData("active", new[] { "C", "A" })]
    [InlineData("", new[] { "C", "B", "A" })]
    public async Task GetList_FiltersByStatus(string status, string[] expectedTitles)
    {
        var client = await factory.CreateAuthorizedClientAsync();
        await client.CreateTaskAsync("A");
        var b = await client.CreateTaskAsync("B");
        await client.CreateTaskAsync("C");
        await client.PatchAsync($"/api/tasks/{b.Id}/done", null);

        var page = await client.GetFromJsonAsync<PagedResponse<TaskResponse>>($"/api/tasks?status={status}");

        Assert.Equal(expectedTitles, page!.Items.Select(t => t.Title));
        Assert.Equal(expectedTitles.Length, page.TotalCount);
    }

    [Fact]
    public async Task GetList_SortsAndPaginates()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        foreach (var title in new[] { "Delta", "Alpha", "Charlie", "Bravo" })
        {
            await client.CreateTaskAsync(title);
        }

        var page = await client.GetFromJsonAsync<PagedResponse<TaskResponse>>("/api/tasks?sort=title&page=2&pageSize=2");

        Assert.Equal(["Charlie", "Delta"], page!.Items.Select(t => t.Title));
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(4, page.TotalCount);
    }

    [Theory]
    [InlineData("status=unknown")]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    public async Task GetList_WithInvalidQuery_Returns400(string query)
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync($"/api/tasks?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
