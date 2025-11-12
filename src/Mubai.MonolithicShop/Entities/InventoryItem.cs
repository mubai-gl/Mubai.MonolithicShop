using System.ComponentModel.DataAnnotations;

namespace Mubai.MonolithicShop.Entities;

/// <summary>
/// 库存实体。
/// </summary>
public class InventoryItem
{
    /// <summary>
    /// 主键。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 商品主键。
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
    /// 关联的商品信息。
    /// </summary>
    public Product? Product { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 乐观锁标记。
    /// </summary>
    [ConcurrencyCheck]
    public int ConcurrencyStamp { get; set; }

    /// <summary>
    /// 可用库存 = 总库存 - 预留库存。
    /// </summary>
    public int AvailableQuantity => QuantityOnHand - ReservedQuantity;
}
