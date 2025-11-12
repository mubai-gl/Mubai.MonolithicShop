using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Services;
using Mubai.MonolithicShop.Tests.TestUtilities;

namespace Mubai.MonolithicShop.Tests.Services;

public class InventoryServiceTests : DatabaseTestBase
{
    public InventoryServiceTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task AdjustInventory_ShouldThrow_WhenResultWouldBeNegative()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ShopDbContext>();
        var inventoryService = services.GetRequiredService<IInventoryService>();

        var product = new Product
        {
            Name = "库存测试商品",
            Sku = "SKU-NEG",
            Price = 10m
        };
        db.Products.Add(product);
        db.InventoryItems.Add(new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = 1
        });
        await db.SaveChangesAsync();

        var request = new AdjustInventoryRequestDto(product.Id, -5);
        Func<Task> act = () => inventoryService.AdjustInventoryAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TryReserveStock_ShouldReturnErrors_WhenInventoryInsufficient()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ShopDbContext>();
        var inventoryService = services.GetRequiredService<IInventoryService>();

        var product = new Product
        {
            Name = "库存不足商品",
            Sku = "SKU-RES",
            Price = 20m
        };
        db.Products.Add(product);
        db.InventoryItems.Add(new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = 1,
            ReservedQuantity = 0
        });
        await db.SaveChangesAsync();

        var requestItems = new[] { new OrderItemRequestDto(product.Id, 5) };

        var result = await inventoryService.TryReserveStockAsync(requestItems, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
