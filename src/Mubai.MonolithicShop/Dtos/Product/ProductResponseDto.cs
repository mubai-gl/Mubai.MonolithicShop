namespace Mubai.MonolithicShop.Dtos.Product
{
    public record ProductResponseDto(Guid Id, string Name, string Sku, decimal Price, string? Description, bool IsActive);

}
