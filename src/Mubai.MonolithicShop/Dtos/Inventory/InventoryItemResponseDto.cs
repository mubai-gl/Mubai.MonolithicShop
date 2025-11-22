namespace Mubai.MonolithicShop.Dtos.Inventory
{
    public record InventoryItemResponseDto(
        Guid ProductId,
        string ProductName,
        int QuantityOnHand,
        int ReservedQuantity);
}
