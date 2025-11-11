using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Services;
using Xunit;

namespace Mubai.MonolithicShop.Tests.Services;

/// <summary>
/// 商品服务的功能测试，验证基础业务逻辑。
/// </summary>
public class ProductServiceTests : IClassFixture<TestUtilities.CustomWebApplicationFactory>
{
    private readonly TestUtilities.CustomWebApplicationFactory _factory;
    private readonly IServiceScopeFactory _scopeFactory;

    public ProductServiceTests(TestUtilities.CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    private async Task ResetDatabaseAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateProductAndInventory()
    {
        await ResetDatabaseAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var dto = new CreateProductRequestDto("测试商品", "SKU-001", 199m, "高端配置");
        var result = await productService.CreateAsync(dto, CancellationToken.None);

        result.Should().NotBeNull();
        result.Sku.Should().Be("SKU-001");

        var inventory = await db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == result.Id);
        inventory.Should().NotBeNull();
        inventory!.QuantityOnHand.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSku_ShouldThrow()
    {
        await ResetDatabaseAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

        var dto = new CreateProductRequestDto("测试商品", "SKU-001", 199m, null);
        await productService.CreateAsync(dto, CancellationToken.None);

        var act = async () => await productService.CreateAsync(dto, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SKU-001*已存在*");
    }
}
