using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos.Identity;
using Mubai.MonolithicShop.Dtos.Order;
using Mubai.MonolithicShop.Dtos.Payment;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Mubai.MonolithicShop.Tests.Integration;

/// <summary>
/// 覆盖全流程的端到端测试，模拟用户登录、下单、支付与库存回收。
/// </summary>
public class OrderWorkflowTests : IClassFixture<TestUtilities.CustomWebApplicationFactory>
{
    private readonly TestUtilities.CustomWebApplicationFactory _factory;

    public OrderWorkflowTests(TestUtilities.CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PlaceOrder_FollowingUserStory_ShouldReachPaidStatus()
    {
        var (userId, productId, email, password) = await SeedUserAndProductAsync();
        var client = _factory.CreateClient();
        var accessToken = await AuthenticateAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new PlaceOrderRequestDto(
            userId,
            new[] { new PlaceOrderItem(productId, 2, 99m) },
            "端到端下单备注",
            new PlaceOrderPaymentDto(198m, "MockGateway", "card", "CNY"));

        var response = await client.PostAsJsonAsync("/api/order", request, CancellationToken.None);
        var payload = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "响应内容: {0}", payload);

        var orderId = await ResolveOrderIdAsync(userId);

        var paymentResponse = await client.PostAsJsonAsync("/api/payment", new ProcessPaymentRequestDto(orderId, 198m, "MockGateway", "card"), CancellationToken.None);
        paymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var order = await db.Orders.Include(o => o.Payment).FirstAsync(o => o.Id == orderId);
        order.Status.Should().Be(OrderStatus.Paid);
        order.Payment.Should().NotBeNull();
        order.Payment!.Status.Should().Be(PaymentStatus.Succeeded);

        var inventory = await db.InventoryItems.FirstAsync(i => i.ProductId == productId);
        inventory.ReservedQuantity.Should().Be(0);
    }

    private async Task<(Guid userId, Guid productId, string email, string password)> SeedUserAndProductAsync()
    {
        await _factory.ResetDatabaseAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string email = "buyer@example.com";
        const string password = "Passw0rd!";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = "下单用户"
        };
        await userManager.CreateAsync(user, password);

        var product = new Product
        {
            Name = "订单测试商品",
            Sku = "SKU-ORDER-001",
            Price = 99m,
            Description = "用于订单流程测试"
        };
        db.Products.Add(product);
        db.InventoryItems.Add(new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = 10,
            ReservedQuantity = 0
        });

        await db.SaveChangesAsync();

        return (user.Id, product.Id, email, password);
    }

    private static async Task<string> AuthenticateAsync(HttpClient client, string email, string password)
    {
        var loginRequest = new LoginDto(email, password);
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest, CancellationToken.None);
        var loginPayload = await loginResponse.Content.ReadAsStringAsync();
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "登录响应: {0}", loginPayload);

        var tokenResponse = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();
        tokenResponse.Should().NotBeNull();
        return tokenResponse!.AccessToken;
    }

    private async Task<long> ResolveOrderIdAsync(Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        return await db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedTime)
            .Select(o => o.Id)
            .FirstAsync();
    }
}
