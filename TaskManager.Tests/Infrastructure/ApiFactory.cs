using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Api.Data;
using Testcontainers.PostgreSql;

namespace TaskManager.Tests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtKey = "integration-tests-signing-key-at-least-32-bytes";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Key", JwtKey);
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
