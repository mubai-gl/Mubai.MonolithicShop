namespace Mubai.MonolithicShop.Entities;

/// <summary>
/// 订单
/// </summary>
public class Order
{
    /// <summary>
    /// 订单主键（雪花 ID）。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 下单用户主键。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 关联用户信息。
    /// </summary>
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// 订单当前状态。
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

    /// <summary>
    /// 订单总金额。
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 货币类型，默认 CNY。
    /// </summary>
    public string Currency { get; set; } = "CNY";

    /// <summary>
    /// 订单备注。
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 支付信息。
    /// </summary>
    public Payment? Payment { get; set; }

    /// <summary>
    /// 订单明细集合。
    /// </summary>
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最近更新时间。
    /// </summary>
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 订单状态枚举。
/// </summary>
public enum OrderStatus
{
    PendingPayment = 0,
    InventoryFailed = 1,
    AwaitingPayment = 2,
    Paid = 3,
    PaymentFailed = 4,
    Cancelled = 5
}
