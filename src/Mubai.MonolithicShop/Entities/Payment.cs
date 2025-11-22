namespace Mubai.MonolithicShop.Entities;

/// <summary>
/// 支付记录实体，关联订单并记录支付状态。
/// </summary>
public class Payment
{
    /// <summary>
    /// 支付记录主键。
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 对应订单主键。
    /// </summary>
    public long OrderId { get; set; }

    /// <summary>
    /// 关联订单信息。
    /// </summary>
    public Order? Order { get; set; }

    /// <summary>
    /// 支付金额。
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 当前支付状态。
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// 支付渠道名称。
    /// </summary>
    public string Provider { get; set; } = "MockGateway";

    /// <summary>
    /// 支付渠道返回的流水号。
    /// </summary>
    public string? ProviderReference { get; set; }

    /// <summary>
    /// 支付失败原因。
    /// </summary>
    public string? FailureReason { get; set; }

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
/// 支付状态。
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2
}
