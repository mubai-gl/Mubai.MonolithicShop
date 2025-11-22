using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mubai.MonolithicShop.Dtos.Product;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Services;
using Mubai.MonolithicShop.Tests.TestUtilities;

namespace Mubai.MonolithicShop.Tests.Services;

/// <summary>
/// 商品服务的功能测试，验证核心业务逻辑。
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

        var dto = new CreateProductRequestDto("测试商品", "SKU-001", 199m, "高端商品");
        await productService.CreateAsync(dto, CancellationToken.None);

        var created = await db.Products.SingleAsync(p => p.Sku == "SKU-001");
        created.Name.Should().Be("测试商品");

        var inventory = await db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == created.Id);
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
            .WithMessage("*SKU-001*");
    }
}
