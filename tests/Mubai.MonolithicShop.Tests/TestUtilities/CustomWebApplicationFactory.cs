using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Infrastructure;

namespace Mubai.MonolithicShop.Tests.TestUtilities;

/// <summary>
/// 自定义 WebApplicationFactory，测试时使用 InMemory 数据库。
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"mubai-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "InMemory",
                ["Database:Provider"] = "InMemory",
                ["Database:ConnectionString"] = _databaseName,
                ["Database:SqliteConnectionString"] = _databaseName,
                ["ConnectionStrings:Default"] = _databaseName
            };
            configBuilder.AddInMemoryCollection(overrides);
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
