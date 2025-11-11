using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Mubai.MonolithicShop.Tests.Integration;

/// <summary>
/// 订单全流程的集成测试，覆盖用户→下单→库存→支付的完整链路。
/// </summary>
public class OrderWorkflowTests : IClassFixture<TestUtilities.CustomWebApplicationFactory>
{
    private readonly TestUtilities.CustomWebApplicationFactory _factory;

    public OrderWorkflowTests(TestUtilities.CustomWebApplicationFactory factory)
    {
        _factory = factory;
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
            Name = "测试商品",
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
        var loginRequest = new LoginRequestDto(email, password);
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest, CancellationToken.None);
        var loginPayload = await loginResponse.Content.ReadAsStringAsync();
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "登录响应: {0}", loginPayload);

        var tokenResponse = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();
        tokenResponse.Should().NotBeNull();
        return tokenResponse!.AccessToken;
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
            new[] { new OrderItemRequestDto(productId, 2) },
            "自动化测试订单",
            new PaymentRequestDto(198m, "MockGateway", "card", "CNY"));

        var response = await client.PostAsJsonAsync("/api/order", request, CancellationToken.None);
        var payload = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, "响应内容: {0}", payload);

        var orderDto = JsonSerializer.Deserialize<OrderResponseDto>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        orderDto.Should().NotBeNull();
        orderDto!.Status.Should().Be(OrderStatus.Paid);
        orderDto.TotalAmount.Should().Be(198m);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var order = await db.Orders.Include(o => o.Payment).FirstAsync(o => o.Id == orderDto.OrderId);
        order.Payment.Should().NotBeNull();
        order.Payment!.Status.Should().Be(PaymentStatus.Succeeded);

        var inventory = await db.InventoryItems.FirstAsync(i => i.ProductId == productId);
        inventory.QuantityOnHand.Should().Be(8);
        inventory.ReservedQuantity.Should().Be(0);
    }
}
