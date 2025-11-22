using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos.Order;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Services;
using Mubai.MonolithicShop.Tests.TestUtilities;

namespace Mubai.MonolithicShop.Tests.Services;

public class OrderServiceTests : DatabaseTestBase
{
    public OrderServiceTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task PlaceOrder_ShouldPersistOrderAndReserveInventory()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ShopDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var orderService = services.GetRequiredService<IOrderService>();

        var user = await CreateUserAsync(userManager, "test@case.com");

        var product = new Product
        {
            Name = "下单测试商品",
            Sku = "SKU-DEBUG",
            Price = 100m
        };
        db.Products.Add(product);
        db.InventoryItems.Add(new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = 5
        });
        await db.SaveChangesAsync();

        var request = new PlaceOrderRequestDto(
            user.Id,
            new[] { new PlaceOrderItem(product.Id, 2, product.Price) },
            null,
            new PlaceOrderPaymentDto(product.Price * 2, "Mock", "card", "CNY"));

        await orderService.PlaceOrderAsync(request, CancellationToken.None);

        var storedOrder = await db.Orders.Include(o => o.Items).SingleAsync();
        storedOrder.Status.Should().Be(OrderStatus.AwaitingPayment);
        storedOrder.TotalAmount.Should().Be(200m);
        storedOrder.Items.Should().ContainSingle(i => i.ProductId == product.Id && i.Quantity == 2);

        var inventory = await db.InventoryItems.SingleAsync(i => i.ProductId == product.Id);
        inventory.ReservedQuantity.Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOrderWithItems()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ShopDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var orderService = services.GetRequiredService<IOrderService>();

        var user = await CreateUserAsync(userManager, "lookup@example.com");
        var product = new Product
        {
            Name = "查询商品",
            Sku = "SKU-LOOKUP",
            Price = 50m
        };
        db.Products.Add(product);
        db.InventoryItems.Add(new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = 3
        });
        await db.SaveChangesAsync();

        var request = new PlaceOrderRequestDto(
            user.Id,
            new[] { new PlaceOrderItem(product.Id, 1, product.Price) },
            "notes",
            new PlaceOrderPaymentDto(product.Price, "Mock", "card", "CNY"));

        await orderService.PlaceOrderAsync(request, CancellationToken.None);

        var orderId = await db.Orders.Select(o => o.Id).SingleAsync();
        var dto = await orderService.GetAsync(orderId, CancellationToken.None);

        dto.Should().NotBeNull();
        dto!.OrderId.Should().Be(orderId);
        dto.Items.Should().ContainSingle(i => i.ProductId == product.Id && i.Quantity == 1);
    }

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email
        };
        var result = await userManager.CreateAsync(user, "Passw0rd!");
        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.Description)));
        return user;
    }
}
