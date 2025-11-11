namespace Mubai.MonolithicShop.Dtos;

public record ProductResponseDto(Guid Id, string Name, string Sku, decimal Price, string? Description, bool IsActive);

public record CreateProductRequestDto(string Name, string Sku, decimal Price, string? Description);

public record UpdateProductRequestDto(Guid Id, string Name, string Sku, decimal Price, string? Description, bool IsActive);
