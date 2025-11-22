using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos.Order;
using Mubai.MonolithicShop.Dtos.Payment;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Services;
using Mubai.MonolithicShop.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Infrastructure;

namespace Mubai.MonolithicShop.Tests.Concurrency;

public class OrderAndPaymentConcurrencyTests : DatabaseTestBase
{
    public OrderAndPaymentConcurrencyTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task PlaceOrder_ShouldHandleConcurrentRequests()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ShopDbContext>();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "concurrent-order@example.com",
            UserName = "concurrent-order@example.com"
        };
        db.Users.Add(user);
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "并发下单商品",
            Sku = "SKU-CONC-ORDER",
            Price = 50m
        };
        db.Products.Add(product);
        db.InventoryItems.Add(new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = 5,
            ReservedQuantity = 0
        });
        await db.SaveChangesAsync();

        var tasks = Enumerable.Range(0, 5).Select(_ => Task.Run(async () =>
        {
            await using var taskScope = ScopeFactory.CreateAsyncScope();
            var orderService = taskScope.ServiceProvider.GetRequiredService<IOrderService>();
            try
            {
                await orderService.PlaceOrderAsync(
                    new PlaceOrderRequestDto(
                        user.Id,
                        new[] { new PlaceOrderItem(product.Id, 2, product.Price) },
                        null,
                        new PlaceOrderPaymentDto(product.Price * 2, "MockGateway", "card", "CNY")),
                    CancellationToken.None);
            }
            catch (DbUpdateConcurrencyException)
            {
                // 并发写入同一库存可能触发并发异常，视为库存不足场景
            }
        }));

        await Task.WhenAll(tasks);

        await using var verifyScope = ScopeFactory.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var orders = await verifyDb.Orders.AsNoTracking().ToListAsync();
        orders.Count.Should().Be(5);

        var inventory = await verifyDb.InventoryItems.AsNoTracking().SingleAsync(i => i.ProductId == product.Id);
        inventory.ReservedQuantity.Should().BeLessThanOrEqualTo(inventory.QuantityOnHand);
    }

    [Fact]
    public async Task ProcessPayment_ShouldBeIdempotentUnderConcurrency()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var orderService = services.GetRequiredService<IOrderService>();
        var db = services.GetRequiredService<ShopDbContext>();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "concurrent-pay@example.com",
            UserName = "concurrent-pay@example.com"
        };
        db.Users.Add(user);
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "并发支付商品",
            Sku = "SKU-CONC-PAY",
            Price = 30m
        };
        db.Products.Add(product);
        db.InventoryItems.Add(new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = 10,
            ReservedQuantity = 0
        });
        await db.SaveChangesAsync();

        await orderService.PlaceOrderAsync(
            new PlaceOrderRequestDto(
                user.Id,
                new[] { new PlaceOrderItem(product.Id, 2, product.Price) },
                null,
                new PlaceOrderPaymentDto(product.Price * 2, "MockGateway", "card", "CNY")),
            CancellationToken.None);

        var orderId = db.Orders.Select(o => o.Id).Single();

        var tasks = Enumerable.Range(0, 3).Select(_ => Task.Run(async () =>
        {
            await using var paymentScope = ScopeFactory.CreateAsyncScope();
            var paymentService = paymentScope.ServiceProvider.GetRequiredService<IPaymentService>();
            await paymentService.ProcessPaymentAsync(
                new ProcessPaymentRequestDto(orderId, product.Price * 2, "MockGateway", "card"),
                CancellationToken.None);
        }));

        await Task.WhenAll(tasks);

        await using var verifyScope = ScopeFactory.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var order = await verifyDb.Orders.AsNoTracking().SingleAsync(o => o.Id == orderId);
        order.Status.Should().Be(OrderStatus.Paid);

        var payment = await verifyDb.Payments.AsNoTracking().SingleAsync(p => p.OrderId == orderId);
        payment.Status.Should().Be(PaymentStatus.Succeeded);

        var inventory = await verifyDb.InventoryItems.AsNoTracking().SingleAsync(i => i.ProductId == product.Id);
        inventory.ReservedQuantity.Should().Be(0);
        inventory.QuantityOnHand.Should().Be(8);
    }
}
