using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Infrastructure;

namespace Mubai.MonolithicShop.Extensions;

public static class MigrationExtensions
{
    /// <summary>
    /// 在开发环境自动执行数据库迁移，避免首次运行缺表。
    /// </summary>
    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbMigrator");

        try
        {
            var dbContext = scope.ServiceProvider.GetService<ShopDbContext>();
            if (dbContext is null)
            {
                return app;
            }

            dbContext.Database.Migrate();
            logger.LogInformation("Applied migrations for context {Context}", typeof(ShopDbContext).Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply migrations for context {Context}", typeof(ShopDbContext).Name);
            throw;
        }

        return app;
    }
}
