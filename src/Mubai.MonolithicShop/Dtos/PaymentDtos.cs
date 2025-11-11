using Mubai.MonolithicShop.Entities;

namespace Mubai.MonolithicShop.Dtos;

public record PaymentRequestDto(decimal Amount, string Provider, string PaymentMethod, string Currency);

public record PaymentResponseDto(Guid PaymentId, PaymentStatus Status, string? ProviderReference, string? FailureReason);
