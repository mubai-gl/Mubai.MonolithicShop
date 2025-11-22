namespace Mubai.MonolithicShop.Dtos.Order
{
    public record PlaceOrderRequestDto(Guid UserId, IReadOnlyCollection<PlaceOrderItem> Items, string? Notes, PlaceOrderPaymentDto Payment);

    public record PlaceOrderItem(Guid ProductId, int Quantity, decimal UnitPrice);

    public record PlaceOrderPaymentDto(decimal Amount, string Provider, string PaymentMethod, string Currency);

}
