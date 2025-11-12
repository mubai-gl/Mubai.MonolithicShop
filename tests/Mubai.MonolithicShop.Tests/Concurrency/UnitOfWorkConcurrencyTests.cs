using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Services;
using Mubai.MonolithicShop.Tests.TestUtilities;

namespace Mubai.MonolithicShop.Tests.Concurrency;

/// <summary>
/// 并发场景下验证 UnitOfWork 的一致性。
/// </summary>
public class UnitOfWorkConcurrencyTests : DatabaseTestBase
{
    public UnitOfWorkConcurrencyTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task UnitOfWork_ShouldHandleConcurrentProductCreation()
    {
        var scopeFactory = ScopeFactory;
        var tasks = Enumerable.Range(0, 10).Select(index => Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
            var dto = new CreateProductRequestDto($"并发商品-{index}", $"SKU-CONCURRENT-{index}", 99m + index, null);
            await productService.CreateAsync(dto, CancellationToken.None);
        }));

        await Task.WhenAll(tasks);

        await using var verifyScope = ScopeFactory.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ShopDbContext>();
        (await verifyDb.Products.CountAsync()).Should().Be(10);
        (await verifyDb.InventoryItems.CountAsync()).Should().Be(10);
    }
}
