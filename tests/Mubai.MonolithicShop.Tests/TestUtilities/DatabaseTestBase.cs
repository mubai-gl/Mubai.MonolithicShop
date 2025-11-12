using Microsoft.Extensions.DependencyInjection;

namespace Mubai.MonolithicShop.Tests.TestUtilities;

/// <summary>
/// 提供数据库清理与作用域辅助的测试基类，避免在每个测试中重复样板代码。
/// </summary>
public abstract class DatabaseTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected DatabaseTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        ScopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    protected CustomWebApplicationFactory Factory { get; }
    protected IServiceScopeFactory ScopeFactory { get; }

    public virtual Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected AsyncServiceScope CreateScope() => Factory.Services.CreateAsyncScope();

    protected Task ExecuteInScopeAsync(Func<IServiceProvider, Task> action) =>
        ExecuteInScopeAsync(async provider =>
        {
            await action(provider);
            return true;
        });

    protected async Task<T> ExecuteInScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }
}
