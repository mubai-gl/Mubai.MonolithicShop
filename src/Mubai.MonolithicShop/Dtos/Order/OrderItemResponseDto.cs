namespace Mubai.MonolithicShop.Dtos.Order
{
    public record OrderItemResponseDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal TotalPrice);
}
