using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using TaskManager.Api.Data;
using TaskManager.Api.Dtos.Auth;
using TaskManager.Tests.Infrastructure;

namespace TaskManager.Tests.Auth;

[Collection(ApiCollection.Name)]
public class AuthEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidData_Returns201AndUser()
    {
        var email = HttpClientExtensions.NewEmail();

        var response = await _client.RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(email, user.Email);
    }

    [Fact]
    public async Task Register_StoresBcryptHashInsteadOfPassword()
    {
        var email = HttpClientExtensions.NewEmail();

        await _client.RegisterAsync(email);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);

        Assert.NotEqual(HttpClientExtensions.Password, user.PasswordHash);
        Assert.StartsWith("$2", user.PasswordHash);
    }

    [Fact]
    public async Task Register_WithTakenEmailInDifferentCase_Returns409()
    {
        var email = HttpClientExtensions.NewEmail();
        await _client.RegisterAsync(email);

        var response = await _client.RegisterAsync(email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", "Passw0rd!")]
    [InlineData("valid@example.com", "short")]
    [InlineData("", "")]
    public async Task Register_WithInvalidData_Returns400(string email, string password)
    {
        var response = await _client.RegisterAsync(email, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.NotEmpty(problem.Errors);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenWithUserClaims()
    {
        var email = HttpClientExtensions.NewEmail();
        var register = await _client.RegisterAsync(email);
        var user = await register.Content.ReadFromJsonAsync<UserResponse>();

        var response = await _client.LoginAsync(email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.True(auth.ExpiresAt > DateTime.UtcNow);

        var token = new JsonWebToken(auth.AccessToken);
        Assert.Equal(user!.Id.ToString(), token.Subject);
        Assert.Equal(email, token.GetClaim(JwtRegisteredClaimNames.Email).Value);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = HttpClientExtensions.NewEmail();
        await _client.RegisterAsync(email);

        var response = await _client.LoginAsync(email, "WrongPassw0rd!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await _client.LoginAsync(HttpClientExtensions.NewEmail());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
