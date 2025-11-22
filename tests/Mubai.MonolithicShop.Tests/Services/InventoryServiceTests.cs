using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Services;
using Mubai.MonolithicShop.Tests.TestUtilities;

namespace Mubai.MonolithicShop.Tests.Services;

public class InventoryServiceTests : DatabaseTestBase
{
    public InventoryServiceTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetInventory_ShouldReturnTrackedItems()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ShopDbContext>();
        var inventoryService = services.GetRequiredService<IInventoryService>();

        var product = new Product
        {
            Name = "库存测试商品",
            Sku = "SKU-INV",
            Price = 10m
        };
        db.Products.Add(product);
        db.InventoryItems.Add(new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = 3,
            ReservedQuantity = 1
        });
        await db.SaveChangesAsync();

        var items = await inventoryService.GetInventoryAsync(CancellationToken.None);

        items.Should().ContainSingle(i =>
            i.ProductId == product.Id &&
            i.QuantityOnHand == 3 &&
            i.ReservedQuantity == 1);
    }
}
