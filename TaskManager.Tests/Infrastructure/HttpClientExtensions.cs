using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManager.Api.Dtos.Auth;
using TaskManager.Api.Dtos.Tasks;

namespace TaskManager.Tests.Infrastructure;

public static class HttpClientExtensions
{
    public const string Password = "Passw0rd!";

    public static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/register", new { email, password });

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    public static async Task<HttpClient> CreateAuthorizedClientAsync(this ApiFactory factory)
    {
        var client = factory.CreateClient();
        var email = NewEmail();

        (await client.RegisterAsync(email)).EnsureSuccessStatusCode();

        var login = await client.LoginAsync(email);
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return client;
    }

    public static async Task<TaskResponse> CreateTaskAsync(this HttpClient client, string title, string? description = null)
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new { title, description });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<TaskResponse>())!;
    }
}
