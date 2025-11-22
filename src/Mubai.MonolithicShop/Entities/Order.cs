using System.ComponentModel.DataAnnotations;

namespace Mubai.MonolithicShop.Entities;

/// <summary>
/// 订单实体。
/// </summary>
public class Order
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "CNY";
    public string? Notes { get; set; }
    public Payment? Payment { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 乐观锁标记，在订单状态变更时递增。
    /// </summary>
    [ConcurrencyCheck]
    public int ConcurrencyStamp { get; set; }
}

public enum OrderStatus
{
    PendingPayment = 0,
    InventoryFailed = 1,
    AwaitingPayment = 2,
    Paid = 3,
    PaymentFailed = 4,
    Cancelled = 5
}
