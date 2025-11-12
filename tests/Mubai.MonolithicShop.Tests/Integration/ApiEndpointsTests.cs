using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Tests.TestUtilities;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Mubai.MonolithicShop.Tests.Integration;

public class ApiEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private const string DefaultPassword = "Passw0rd!";

    public ApiEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Auth_Login_ShouldReturnTokens()
    {
        var user = await SeedUserAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(user.Email!, DefaultPassword), CancellationToken.None);

        var tokens = await ReadResponseAsync<TokenResponseDto>(response, HttpStatusCode.OK);
        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Auth_Refresh_ShouldIssueNewToken()
    {
        var user = await SeedUserAsync();
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(user.Email!, DefaultPassword), CancellationToken.None);
        var tokens = await ReadResponseAsync<TokenResponseDto>(loginResponse, HttpStatusCode.OK);

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto(tokens.RefreshToken), CancellationToken.None);
        var refreshed = await ReadResponseAsync<TokenResponseDto>(refreshResponse, HttpStatusCode.OK);
        refreshed.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Auth_Register_ShouldCreateUser()
    {
        var client = _factory.CreateClient();
        var email = $"public-register-{Guid.NewGuid():N}@example.com";
        var request = new CreateUserRequestDto(email, "公开注册用户", "RegPassw0rd!", "13888888888");

        var response = await client.PostAsJsonAsync("/api/auth/register", request, CancellationToken.None);

        var dto = await ReadResponseAsync<UserResponseDto>(response, HttpStatusCode.Created);
        dto.Email.Should().Be(email);
    }

    [Fact]
    public async Task Inventory_GetAll_ShouldReturnItems()
    {
        var (client, product) = await CreateAuthorizedClientWithProductAsync();

        var response = await client.GetAsync("/api/inventory");

        var items = await ReadResponseAsync<List<InventoryItemResponseDto>>(response, HttpStatusCode.OK);
        items.Should().Contain(i => i.ProductId == product.Id && i.QuantityOnHand == 10);
    }

    [Fact]
    public async Task Inventory_Adjust_ShouldUpdateQuantity()
    {
        var (client, product) = await CreateAuthorizedClientWithProductAsync();

        var response = await client.PostAsJsonAsync("/api/inventory/adjust", new AdjustInventoryRequestDto(product.Id, 5), CancellationToken.None);

        var item = await ReadResponseAsync<InventoryItemResponseDto>(response, HttpStatusCode.OK);
        item.QuantityOnHand.Should().Be(15);
    }

    [Fact]
    public async Task Product_GetAll_ShouldReturnProducts()
    {
        var (client, product) = await CreateAuthorizedClientWithProductAsync();

        var response = await client.GetAsync("/api/product");

        var products = await ReadResponseAsync<List<ProductResponseDto>>(response, HttpStatusCode.OK);
        products.Should().Contain(p => p.Id == product.Id);
    }

    [Fact]
    public async Task Product_Get_ShouldReturnSingleProduct()
    {
        var (client, product) = await CreateAuthorizedClientWithProductAsync();

        var response = await client.GetAsync($"/api/product/{product.Id}");

        var dto = await ReadResponseAsync<ProductResponseDto>(response, HttpStatusCode.OK);
        dto.Id.Should().Be(product.Id);
    }

    [Fact]
    public async Task Product_Create_ShouldSucceed()
    {
        var (client, _) = await CreateAuthorizedClientAsync();
        var request = new CreateProductRequestDto("API 创建商品", $"SKU-{Guid.NewGuid():N}", 199m, "测试商品");

        var response = await client.PostAsJsonAsync("/api/product", request, CancellationToken.None);

        var dto = await ReadResponseAsync<ProductResponseDto>(response, HttpStatusCode.Created);
        dto.Name.Should().Be("API 创建商品");
    }

    [Fact]
    public async Task Product_Update_ShouldReturnUpdatedProduct()
    {
        var (client, product) = await CreateAuthorizedClientWithProductAsync();
        var request = new UpdateProductRequestDto(product.Id, "更新后的商品", product.Sku, 299m, "更新描述", false);

        var response = await client.PutAsJsonAsync($"/api/product/{product.Id}", request, CancellationToken.None);

        var dto = await ReadResponseAsync<ProductResponseDto>(response, HttpStatusCode.OK);
        dto.Name.Should().Be("更新后的商品");
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Product_Endpoints_ShouldRejectUnauthenticatedRequests()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/product");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Order_PlaceOrder_ShouldReturnCreatedOrder()
    {
        var (client, user) = await CreateAuthorizedClientAsync();
        var product = await SeedProductWithInventoryAsync();

        var response = await client.PostAsJsonAsync("/api/order",
            new PlaceOrderRequestDto(
                user.Id,
                new[] { new OrderItemRequestDto(product.Id, 1) },
                "下单测试",
                new PaymentRequestDto(product.Price, "MockGateway", "card", "CNY")),
            CancellationToken.None);

        var order = await ReadResponseAsync<OrderResponseDto>(response, HttpStatusCode.Created);
        order.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task Order_PlaceOrder_ShouldReturnPaymentFailed_WhenGatewayRejects()
    {
        var (client, user) = await CreateAuthorizedClientAsync();
        var product = await SeedProductWithInventoryAsync();

        var response = await client.PostAsJsonAsync("/api/order",
            new PlaceOrderRequestDto(
                user.Id,
                new[] { new OrderItemRequestDto(product.Id, 1) },
                "支付失败",
                new PaymentRequestDto(product.Price, "MockGateway", "simulate-failure", "CNY")),
            CancellationToken.None);

        var order = await ReadResponseAsync<OrderResponseDto>(response, HttpStatusCode.Created);
        order.Status.Should().Be(OrderStatus.PaymentFailed);
    }

    [Fact]
    public async Task Order_PlaceOrder_ShouldReturnProblemDetails_WhenInventoryInsufficient()
    {
        var (client, user) = await CreateAuthorizedClientAsync();
        var product = await SeedProductWithInventoryAsync(quantityOnHand: 1);

        var response = await client.PostAsJsonAsync("/api/order",
            new PlaceOrderRequestDto(
                user.Id,
                new[] { new OrderItemRequestDto(product.Id, 5) },
                "库存不足",
                new PaymentRequestDto(product.Price * 5, "MockGateway", "card", "CNY")),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problem.Detail.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Order_GetById_ShouldReturnOrder()
    {
        var (client, user) = await CreateAuthorizedClientAsync();
        var product = await SeedProductWithInventoryAsync();
        var order = await PlaceOrderViaApiAsync(client, user.Id, product);

        var response = await client.GetAsync($"/api/order/{order.OrderId}");

        var dto = await ReadResponseAsync<OrderResponseDto>(response, HttpStatusCode.OK);
        dto.OrderId.Should().Be(order.OrderId);
    }

    [Fact]
    public async Task Payment_GetByOrder_ShouldReturnPayment()
    {
        var (client, user) = await CreateAuthorizedClientAsync();
        var product = await SeedProductWithInventoryAsync();
        var order = await PlaceOrderViaApiAsync(client, user.Id, product);

        var response = await client.GetAsync($"/api/payment/{order.OrderId}");

        var payment = await ReadResponseAsync<PaymentResponseDto>(response, HttpStatusCode.OK);
        payment.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact]
    public async Task User_Register_ShouldCreateUser()
    {
        var (client, _) = await CreateAuthorizedClientAsync();
        var email = $"newuser-{Guid.NewGuid():N}@example.com";
        var request = new CreateUserRequestDto(email, "注册用户", "RegPassw0rd!", "13800000000");

        var response = await client.PostAsJsonAsync("/api/user", request, CancellationToken.None);

        var dto = await ReadResponseAsync<UserResponseDto>(response, HttpStatusCode.Created);
        dto.Email.Should().Be(email);
    }

    [Fact]
    public async Task User_Get_ShouldReturnUser()
    {
        var extraUser = await SeedUserAsync();
        var (client, _) = await CreateAuthorizedClientAsync(extraUser);

        var response = await client.GetAsync($"/api/user/{extraUser.Id}");

        var dto = await ReadResponseAsync<UserResponseDto>(response, HttpStatusCode.OK);
        dto.Id.Should().Be(extraUser.Id);
    }

    [Fact]
    public async Task User_List_ShouldReturnUsers()
    {
        var adminUser = await SeedUserAsync();
        var (client, _) = await CreateAuthorizedClientAsync(adminUser);
        await SeedUserAsync();
        await SeedUserAsync();

        var response = await client.GetAsync("/api/user");

        var users = await ReadResponseAsync<List<UserResponseDto>>(response, HttpStatusCode.OK);
        users.Count.Should().BeGreaterThanOrEqualTo(3); // 包含授权用户及新增用户
    }

    private async Task<ApplicationUser> SeedUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"tester-{Guid.NewGuid():N}@example.com";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = "集成测试用户"
        };

        var result = await userManager.CreateAsync(user, DefaultPassword);
        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.Description)));

        return user;
    }

    private async Task<(HttpClient Client, ApplicationUser User)> CreateAuthorizedClientAsync(ApplicationUser? existingUser = null)
    {
        var client = _factory.CreateClient();
        var user = existingUser ?? await SeedUserAsync();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(user.Email!, DefaultPassword), CancellationToken.None);
        var tokens = await ReadResponseAsync<TokenResponseDto>(loginResponse, HttpStatusCode.OK);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return (client, user);
    }

    private async Task<(HttpClient Client, Product Product)> CreateAuthorizedClientWithProductAsync(int quantityOnHand = 10)
    {
        var (client, _) = await CreateAuthorizedClientAsync();
        var product = await SeedProductWithInventoryAsync(quantityOnHand);
        return (client, product);
    }

    private async Task<Product> SeedProductWithInventoryAsync(int quantityOnHand = 10)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var product = new Product
        {
            Name = "集成测试商品",
            Sku = $"SKU-{Guid.NewGuid():N}",
            Price = 150m,
            Description = "用于 WebAPI 集成测试"
        };

        db.Products.Add(product);
        db.InventoryItems.Add(new InventoryItem
        {
            ProductId = product.Id,
            QuantityOnHand = quantityOnHand,
            ReservedQuantity = 0
        });

        await db.SaveChangesAsync();
        return product;
    }

    private static async Task<TResponse> ReadResponseAsync<TResponse>(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        response.StatusCode.Should().Be(expectedStatus);
        var payload = await response.Content.ReadFromJsonAsync<TResponse>();
        payload.Should().NotBeNull();
        return payload!;
    }

    private static async Task<OrderResponseDto> PlaceOrderViaApiAsync(HttpClient client, Guid userId, Product product, int quantity = 1)
    {
        var request = new PlaceOrderRequestDto(
            userId,
            new[] { new OrderItemRequestDto(product.Id, quantity) },
            "集成测试下单",
            new PaymentRequestDto(product.Price * quantity, "MockGateway", "card", "CNY"));

        var response = await client.PostAsJsonAsync("/api/order", request, CancellationToken.None);
        return await ReadResponseAsync<OrderResponseDto>(response, HttpStatusCode.Created);
    }
}
