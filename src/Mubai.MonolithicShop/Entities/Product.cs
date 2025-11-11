namespace Mubai.MonolithicShop.Entities;

/// <summary>
/// 商品
/// </summary>
public class Product
{
    /// <summary>
    /// 商品主键。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 商品名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 商品描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 商品 SKU，用于唯一标识。
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// 商品单价。
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 是否上架。
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 订单明细集合。
    /// </summary>
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    /// <summary>
    /// 关联的库存记录。
    /// </summary>
    public InventoryItem? InventoryItem { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最近更新时间。
    /// </summary>
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
}
