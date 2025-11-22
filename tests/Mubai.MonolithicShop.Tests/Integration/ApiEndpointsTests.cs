using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop;
using Mubai.MonolithicShop.Dtos.Identity;
using Mubai.MonolithicShop.Dtos.Inventory;
using Mubai.MonolithicShop.Dtos.Order;
using Mubai.MonolithicShop.Dtos.Payment;
using Mubai.MonolithicShop.Dtos.Product;
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

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto(user.Email!, DefaultPassword), CancellationToken.None);

        var tokens = await ReadResponseAsync<TokenResponseDto>(response, HttpStatusCode.OK);
        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Auth_Refresh_ShouldIssueNewToken()
    {
        var user = await SeedUserAsync();
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginDto(user.Email!, DefaultPassword), CancellationToken.None);
        var tokens = await ReadResponseAsync<TokenResponseDto>(loginResponse, HttpStatusCode.OK);

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenDto(tokens.RefreshToken), CancellationToken.None);
        var refreshed = await ReadResponseAsync<TokenResponseDto>(refreshResponse, HttpStatusCode.OK);
        refreshed.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Auth_Register_ShouldCreateUser()
    {
        var client = _factory.CreateClient();
        var email = $"public-register-{Guid.NewGuid():N}@example.com";
        var request = new CreateUserDto(email, "����ע���û�", "RegPassw0rd!", "13888888888");

        var response = await client.PostAsJsonAsync("/api/auth/register", request, CancellationToken.None);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var created = await userManager.FindByEmailAsync(email);
        created.Should().NotBeNull();
    }

    [Fact]
    public async Task Product_CreateAndGet_ShouldWork()
    {
        var (client, _) = await CreateAuthorizedClientAsync();
        var request = new CreateProductRequestDto("API ������Ʒ", $"SKU-{Guid.NewGuid():N}", 199m, "������Ʒ");

        var response = await client.PostAsJsonAsync("/api/product", request, CancellationToken.None);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var productsResponse = await client.GetAsync("/api/product");
        var products = await ReadResponseAsync<List<ProductResponseDto>>(productsResponse, HttpStatusCode.OK);
        products.Should().Contain(p => p.Sku == request.Sku && p.Price == request.Price);
    }

    [Fact]
    public async Task Product_Update_ShouldPersistChanges()
    {
        var (client, product) = await CreateAuthorizedClientWithProductAsync();
        var request = new UpdateProductRequestDto(product.Id, "���º����Ʒ", product.Sku, 299m, "��������", false);

        var response = await client.PutAsJsonAsync("/api/product", request, CancellationToken.None);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/api/product/{product.Id}");
        var dto = await ReadResponseAsync<ProductResponseDto>(getResponse, HttpStatusCode.OK);
        dto.Name.Should().Be("���º����Ʒ");
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
    public async Task Inventory_GetAll_ShouldReturnItems()
    {
        var (client, product) = await CreateAuthorizedClientWithProductAsync();

        var response = await client.GetAsync("/api/inventory");

        var items = await ReadResponseAsync<List<InventoryItemResponseDto>>(response, HttpStatusCode.OK);
        items.Should().Contain(i => i.ProductId == product.Id && i.QuantityOnHand == 10);
    }

    [Fact]
    public async Task Order_PlaceAndPay_ShouldUpdateOrderAndPaymentStatuses()
    {
        var (client, user) = await CreateAuthorizedClientAsync();
        var product = await SeedProductWithInventoryAsync();

        var placeRequest = new PlaceOrderRequestDto(
            user.Id,
            new[] { new PlaceOrderItem(product.Id, 1, product.Price) },
            "�µ�����",
            new PlaceOrderPaymentDto(product.Price, "MockGateway", "card", "CNY"));

        var placeResponse = await client.PostAsJsonAsync("/api/order", placeRequest, CancellationToken.None);
        placeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderId = await ResolveLatestOrderIdAsync(user.Id);

        var paymentResponse = await client.PostAsJsonAsync("/api/payment", new ProcessPaymentRequestDto(orderId, product.Price, "MockGateway", "card"), CancellationToken.None);
        paymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderResponse = await client.GetAsync($"/api/order/{orderId}");
        var order = await ReadResponseAsync<OrderResponseDto>(orderResponse, HttpStatusCode.OK);
        order.Status.Should().Be(OrderStatus.Paid);

        var payment = await client.GetFromJsonAsync<PaymentResponseDto>($"/api/payment/{orderId}");
        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact]
    public async Task Payment_Process_ShouldSetFailureStatus_WhenGatewayRejects()
    {
        var (client, user) = await CreateAuthorizedClientAsync();
        var product = await SeedProductWithInventoryAsync();

        var placeRequest = new PlaceOrderRequestDto(
            user.Id,
            new[] { new PlaceOrderItem(product.Id, 1, product.Price) },
            "֧��ʧ��",
            new PlaceOrderPaymentDto(product.Price, "MockGateway", "card", "CNY"));

        var placeResponse = await client.PostAsJsonAsync("/api/order", placeRequest, CancellationToken.None);
        placeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderId = await ResolveLatestOrderIdAsync(user.Id);

        var paymentResponse = await client.PostAsJsonAsync("/api/payment", new ProcessPaymentRequestDto(orderId, product.Price, "MockGateway", "simulate-failure"), CancellationToken.None);
        paymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payment = await client.GetFromJsonAsync<PaymentResponseDto>($"/api/payment/{orderId}");
        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Failed);
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
        users.Count.Should().BeGreaterThanOrEqualTo(3);
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
            DisplayName = "���ɲ����û�"
        };

        var result = await userManager.CreateAsync(user, DefaultPassword);
        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.Description)));

        return user;
    }

    private async Task<(HttpClient Client, ApplicationUser User)> CreateAuthorizedClientAsync(ApplicationUser? existingUser = null)
    {
        var client = _factory.CreateClient();
        var user = existingUser ?? await SeedUserAsync();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginDto(user.Email!, DefaultPassword), CancellationToken.None);
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
            Name = "���ɲ�����Ʒ",
            Sku = $"SKU-{Guid.NewGuid():N}",
            Price = 150m,
            Description = "���� WebAPI ���ɲ���"
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

    private async Task<long> ResolveLatestOrderIdAsync(Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        return await db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedTime)
            .Select(o => o.Id)
            .FirstAsync();
    }

    private static async Task<TResponse> ReadResponseAsync<TResponse>(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        response.StatusCode.Should().Be(expectedStatus);
        var payload = await response.Content.ReadFromJsonAsync<TResponse>();
        payload.Should().NotBeNull();
        return payload!;
    }
}
