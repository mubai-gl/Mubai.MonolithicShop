namespace Mubai.MonolithicShop.Entities;

/// <summary>
/// 库存
/// </summary>
public class InventoryItem
{
    /// <summary>
    /// 主键。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 对应的商品主键。
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// 当前可用库存数量。
    /// </summary>
    public int QuantityOnHand { get; set; }

    /// <summary>
    /// 已被订单预留的数量。
    /// </summary>
    public int ReservedQuantity { get; set; }

    /// <summary>
    /// 关联商品信息。
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最近更新时间。
    /// </summary>
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 计算可用库存 = 总库存 - 预留。
    /// </summary>
    public int AvailableQuantity => QuantityOnHand - ReservedQuantity;
}
