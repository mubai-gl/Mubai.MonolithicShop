using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Dtos.Payment;

public record PaymentResponseDto(Guid Id, PaymentStatus Status, string? ProviderReference, string? FailureReason);
//public record PaymentResponseDto(Guid PaymentId, PaymentStatus Status, string? ProviderReference, string? FailureReason);
