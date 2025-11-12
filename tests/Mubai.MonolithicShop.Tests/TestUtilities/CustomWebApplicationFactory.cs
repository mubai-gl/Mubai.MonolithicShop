using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mubai.MonolithicShop.Tests.TestUtilities;

/// <summary>
/// 自定义 WebApplicationFactory，测试时使用 SQLite 文件数据库。
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"mubai-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "Sqlite",
                ["Database:Provider"] = "Sqlite",
                ["Database:ConnectionString"] = $"Data Source={_dbPath}",
                ["Database:SqliteConnectionString"] = $"Data Source={_dbPath}",
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}"
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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch (IOException)
            {
                // 测试运行结束时若文件被占用，忽略删除异常。
            }
        }
    }
}
