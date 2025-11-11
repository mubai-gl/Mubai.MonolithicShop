namespace Mubai.MonolithicShop.Entities;

/// <summary>
/// 订单明细实体，记录单个商品的下单信息。
/// </summary>
public class OrderItem
{
    /// <summary>
    /// 明细主键。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属订单主键。
    /// </summary>
    public long OrderId { get; set; }

    /// <summary>
    /// 关联订单。
    /// </summary>
    public Order? Order { get; set; }

    /// <summary>
    /// 商品主键。
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// 商品信息。
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// 购买数量。
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 商品单价。
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最近更新时间。
    /// </summary>
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 计算行小计 = 数量 * 单价。
    /// </summary>
    public decimal TotalPrice => Quantity * UnitPrice;
}
