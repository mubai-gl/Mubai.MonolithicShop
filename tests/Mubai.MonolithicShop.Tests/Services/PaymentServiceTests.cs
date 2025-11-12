using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos;
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
        var db = services.GetRequiredService<ShopDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var paymentService = services.GetRequiredService<IPaymentService>();

        var order = await SeedOrderAsync(db, userManager, 200m);
        var request = new PaymentRequestDto(150m, "MockGateway", "card", "CNY");

        var response = await paymentService.ProcessPaymentAsync(order, request, CancellationToken.None);

        response.Status.Should().Be(PaymentStatus.Failed);
        response.FailureReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ProcessPayment_ShouldFail_WhenSimulatedFailureRequested()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ShopDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var paymentService = services.GetRequiredService<IPaymentService>();

        var order = await SeedOrderAsync(db, userManager, 80m);
        var request = new PaymentRequestDto(order.TotalAmount, "MockGateway", "simulate-failure", "CNY");

        var response = await paymentService.ProcessPaymentAsync(order, request, CancellationToken.None);

        response.Status.Should().Be(PaymentStatus.Failed);
        response.FailureReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ProcessPayment_ShouldUpdateExistingRecord_WhenPaymentProcessedAgain()
    {
        await using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ShopDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var paymentService = services.GetRequiredService<IPaymentService>();
        var paymentRepository = services.GetRequiredService<IPaymentRepository>();

        var order = await SeedOrderAsync(db, userManager, 120m);
        var initialRequest = new PaymentRequestDto(order.TotalAmount, "Gateway-A", "card", "CNY");
        var secondRequest = initialRequest with { Provider = "Gateway-B", Currency = "USD" };

        var first = await paymentService.ProcessPaymentAsync(order, initialRequest, CancellationToken.None);
        first.Status.Should().Be(PaymentStatus.Succeeded);

        var second = await paymentService.ProcessPaymentAsync(order, secondRequest, CancellationToken.None);
        second.Status.Should().Be(PaymentStatus.Succeeded);

        var stored = await paymentRepository.GetByOrderIdAsync(order.Id, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Provider.Should().Be("Gateway-B");
        stored.Currency.Should().Be("USD");
    }

    private static async Task<Order> SeedOrderAsync(ShopDbContext db, UserManager<ApplicationUser> userManager, decimal totalAmount)
    {
        var email = $"payment-{Guid.NewGuid():N}@example.com";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email
        };
        var createResult = await userManager.CreateAsync(user, "Passw0rd!");
        createResult.Succeeded.Should().BeTrue(string.Join(";", createResult.Errors.Select(e => e.Description)));

        var order = new Order
        {
            Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UserId = user.Id,
            TotalAmount = totalAmount,
            Status = OrderStatus.AwaitingPayment
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order;
    }
}
