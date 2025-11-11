using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Dtos;

public record OrderItemRequestDto(Guid ProductId, int Quantity);

public record PlaceOrderRequestDto(Guid UserId, IReadOnlyCollection<OrderItemRequestDto> Items, string? Notes, PaymentRequestDto Payment);

public record OrderItemResponseDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal TotalPrice);

public record OrderResponseDto(long OrderId, OrderStatus Status, decimal TotalAmount, IReadOnlyCollection<OrderItemResponseDto> Items);
