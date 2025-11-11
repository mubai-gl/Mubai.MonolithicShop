namespace Mubai.MonolithicShop.Dtos;

public record InventoryItemResponseDto(
    Guid ProductId,
    string ProductName,
    int QuantityOnHand,
    int ReservedQuantity);

public record AdjustInventoryRequestDto(Guid ProductId, int QuantityDelta);

public record ReserveStockRequestDto(Guid ProductId, int Quantity);

public record InventoryReservationResultDto(bool Success, IReadOnlyCollection<string> Errors);
