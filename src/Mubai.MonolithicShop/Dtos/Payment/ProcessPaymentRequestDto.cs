namespace Mubai.MonolithicShop.Dtos.Payment;

public record ProcessPaymentRequestDto(long OrderId, decimal Amount, string Provider, string PaymentMethod);
