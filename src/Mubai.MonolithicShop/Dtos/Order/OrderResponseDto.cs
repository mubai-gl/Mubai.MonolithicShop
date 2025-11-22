using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Dtos.Order
{
    public record OrderResponseDto(long OrderId, OrderStatus Status, decimal TotalAmount, IReadOnlyCollection<OrderItemResponseDto> Items);
}
