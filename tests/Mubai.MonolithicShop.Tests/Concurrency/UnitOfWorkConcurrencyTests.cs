using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Services;
using Xunit;

namespace Mubai.MonolithicShop.Tests.Concurrency;

/// <summary>
/// 并发场景下验证 UnitOfWork 的一致性。
/// </summary>
public class UnitOfWorkConcurrencyTests : IClassFixture<TestUtilities.CustomWebApplicationFactory>
{
    private readonly TestUtilities.CustomWebApplicationFactory _factory;
    private readonly IServiceScopeFactory _scopeFactory;

    public UnitOfWorkConcurrencyTests(TestUtilities.CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task UnitOfWork_ShouldHandleConcurrentProductCreation()
    {
        await _factory.ResetDatabaseAsync();

        var scopeFactory = _scopeFactory;
        var tasks = Enumerable.Range(0, 10).Select(index => Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
            var dto = new CreateProductRequestDto($"并发商品-{index}", $"SKU-CONCURRENT-{index}", 99m + index, null);
            await productService.CreateAsync(dto, CancellationToken.None);
        }));

        await Task.WhenAll(tasks);

        await using var verifyScope = _scopeFactory.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ShopDbContext>();
        (await verifyDb.Products.CountAsync()).Should().Be(10);
        (await verifyDb.InventoryItems.CountAsync()).Should().Be(10);
    }
}
