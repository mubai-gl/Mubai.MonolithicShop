using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Services;
using Mubai.MonolithicShop.Tests.TestUtilities;

namespace Mubai.MonolithicShop.Tests.Services;

/// <summary>
/// 商品服务的功能测试，验证基础业务逻辑。
/// </summary>
public class ProductServiceTests : DatabaseTestBase
{
    public ProductServiceTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateProductAndInventory()
    {
        await using var scope = CreateScope();
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
        await using var scope = CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

        var dto = new CreateProductRequestDto("测试商品", "SKU-001", 199m, null);
        await productService.CreateAsync(dto, CancellationToken.None);

        var act = async () => await productService.CreateAsync(dto, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SKU-001*已存在*");
    }
}
