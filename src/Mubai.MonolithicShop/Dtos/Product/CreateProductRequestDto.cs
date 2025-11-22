namespace Mubai.MonolithicShop.Dtos.Product
{
    public record CreateProductRequestDto(string Name, string Sku, decimal Price, string? Description);
}
