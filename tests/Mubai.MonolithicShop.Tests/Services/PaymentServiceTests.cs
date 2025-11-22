using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop;
using Mubai.MonolithicShop.Dtos.Payment;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Repositories;
using Mubai.MonolithicShop.Services;
using Mubai.MonolithicShop.Tests.TestUtilities;

namespace Mubai.MonolithicShop.Tests.Services;

public class PaymentServiceTests : DatabaseTestBase
{
    public PaymentServiceTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ProcessPayment_ShouldFail_WhenAmountMismatch()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var paymentService = services.GetRequiredService<IPaymentService>();
        var paymentRepository = services.GetRequiredService<IPaymentRepository>();
        var order = await SeedOrderAsync(services, 200m);

        await paymentService.ProcessPaymentAsync(
            new ProcessPaymentRequestDto(order.Id, 150m, "MockGateway", "card"),
            CancellationToken.None);

        var payment = await paymentRepository.GetByOrderIdAsync(order.Id, CancellationToken.None);
        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ProcessPayment_ShouldFail_WhenSimulatedFailureRequested()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var paymentService = services.GetRequiredService<IPaymentService>();
        var paymentRepository = services.GetRequiredService<IPaymentRepository>();
        var order = await SeedOrderAsync(services, 80m);

        await paymentService.ProcessPaymentAsync(
            new ProcessPaymentRequestDto(order.Id, order.TotalAmount, "MockGateway", "simulate-failure"),
            CancellationToken.None);

        var payment = await paymentRepository.GetByOrderIdAsync(order.Id, CancellationToken.None);
        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ProcessPayment_ShouldUpdateExistingRecord_WhenPaymentProcessedAgain()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var paymentService = services.GetRequiredService<IPaymentService>();
        var paymentRepository = services.GetRequiredService<IPaymentRepository>();
        var order = await SeedOrderAsync(services, 120m);

        await paymentService.ProcessPaymentAsync(
            new ProcessPaymentRequestDto(order.Id, order.TotalAmount, "Gateway-A", "card"),
            CancellationToken.None);
        await paymentService.ProcessPaymentAsync(
            new ProcessPaymentRequestDto(order.Id, order.TotalAmount, "Gateway-B", "card"),
            CancellationToken.None);

        var stored = await paymentRepository.GetByOrderIdAsync(order.Id, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Provider.Should().Be("Gateway-A");
        stored.Status.Should().Be(PaymentStatus.Succeeded);
    }

    private static async Task<Order> SeedOrderAsync(IServiceProvider services, decimal totalAmount)
    {
        var db = services.GetRequiredService<ShopDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"payment-{Guid.NewGuid():N}@example.com";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email
        };
        var createResult = await userManager.CreateAsync(user, "Passw0rd!");
        createResult.Succeeded.Should().BeTrue(string.Join(";", createResult.Errors.Select(e => e.Description)));

        var product = new Product
        {
            Name = "֧������Ʒ",
            Sku = $"SKU-PAY-{Guid.NewGuid():N}".Substring(0, 16),
            Price = totalAmount
        };
        db.Products.Add(product);

        var orderId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var order = new Order
        {
            Id = orderId,
            UserId = user.Id,
            TotalAmount = totalAmount,
            Status = OrderStatus.AwaitingPayment,
            Items = new List<OrderItem>
            {
                new()
                {
                    OrderId = orderId,
                    ProductId = product.Id,
                    Quantity = 1,
                    UnitPrice = totalAmount
                }
            }
        };

        db.Orders.Add(order);
        db.InventoryItems.Add(new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = 5,
            ReservedQuantity = 2
        });

        await db.SaveChangesAsync();
        return order;
    }
}
