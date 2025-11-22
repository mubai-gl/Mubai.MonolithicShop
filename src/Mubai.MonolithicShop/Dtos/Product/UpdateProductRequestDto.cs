namespace Mubai.MonolithicShop.Dtos.Product
{
    public record UpdateProductRequestDto(Guid Id, string Name, string Sku, decimal Price, string? Description, bool IsActive);
}
