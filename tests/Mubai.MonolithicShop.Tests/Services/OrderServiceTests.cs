using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Services;
using Mubai.MonolithicShop.Tests.TestUtilities;

namespace Mubai.MonolithicShop.Tests.Services;

public class OrderServiceTests : DatabaseTestBase
{
    public OrderServiceTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task PlaceOrder_ShouldSucceed()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ShopDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var orderService = services.GetRequiredService<IOrderService>();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@case.com",
            UserName = "test@case.com"
        };
        await userManager.CreateAsync(user, "Passw0rd!");

        var product = new Product
        {
            Name = "调试商品",
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
            new[] { new OrderItemRequestDto(product.Id, 1) },
            null,
            new PaymentRequestDto(100m, "Mock", "card", "CNY"));

        var result = await orderService.PlaceOrderAsync(request, CancellationToken.None);
        result.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task PlaceOrder_ShouldFail_WhenInventoryUnavailable()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ShopDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var orderService = services.GetRequiredService<IOrderService>();

        var user = await CreateUserAsync(userManager, "inventory-fail@example.com");
        var product = new Product
        {
            Name = "库存不足商品",
            Sku = "SKU-NO-STOCK",
            Price = 50m
        };
        db.Products.Add(product);
        db.InventoryItems.Add(new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = 0
        });
        await db.SaveChangesAsync();

        var request = new PlaceOrderRequestDto(
            user.Id,
            new[] { new OrderItemRequestDto(product.Id, 1) },
            null,
            new PaymentRequestDto(50m, "Mock", "card", "CNY"));

        var act = () => orderService.PlaceOrderAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        var storedOrder = await db.Orders.SingleAsync();
        storedOrder.Status.Should().Be(OrderStatus.InventoryFailed);
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnPaymentFailed_WhenProcessorRejects()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ShopDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var orderService = services.GetRequiredService<IOrderService>();

        var user = await CreateUserAsync(userManager, "payment-fail@example.com");
        var product = new Product
        {
            Name = "支付失败测试商品",
            Sku = "SKU-PAYFAIL",
            Price = 99m
        };
        db.Products.Add(product);
        db.InventoryItems.Add(new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = 10
        });
        await db.SaveChangesAsync();

        var request = new PlaceOrderRequestDto(
            user.Id,
            new[] { new OrderItemRequestDto(product.Id, 1) },
            "trigger payment failure",
            new PaymentRequestDto(product.Price, "Mock", "simulate-failure", "CNY"));

        var response = await orderService.PlaceOrderAsync(request, CancellationToken.None);

        response.Status.Should().Be(OrderStatus.PaymentFailed);
        var storedOrder = await db.Orders.SingleAsync(o => o.Id == response.OrderId);
        storedOrder.Status.Should().Be(OrderStatus.PaymentFailed);
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
