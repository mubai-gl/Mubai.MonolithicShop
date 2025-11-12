using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Repositories;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 支付服务，负责模拟支付渠道并记录结果。
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 处理支付请求，可按参数模拟失败或成功。
    /// </summary>
    public async Task<PaymentResponseDto> ProcessPaymentAsync(Order order, PaymentRequestDto request, CancellationToken token = default)
    {
        var (payment, isNew) = await LoadOrCreatePaymentAsync(order, request, token);
        DeterminePaymentResult(order, request, payment);

        if (!isNew)
        {
            _paymentRepository.Update(payment);
        }
        await _unitOfWork.SaveChangesAsync(token);

        if (payment.Status == PaymentStatus.Succeeded)
        {
            _logger.LogInformation("支付记录 {PaymentId} 对应订单 {OrderId} 支付成功。", payment.Id, order.Id);
        }
        else
        {
            _logger.LogWarning("支付记录 {PaymentId} 对应订单 {OrderId} 支付失败：{Reason}", payment.Id, order.Id, payment.FailureReason);
        }

        return Map(payment);
    }

    /// <summary>
    /// 根据订单编号查询支付结果。
    /// </summary>
    public async Task<PaymentResponseDto?> GetByOrderAsync(long orderId, CancellationToken token = default)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(orderId, token);
        return payment is null ? null : Map(payment);
    }

    private async Task<(Payment payment, bool isNew)> LoadOrCreatePaymentAsync(Order order, PaymentRequestDto request, CancellationToken token)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(order.Id, token);
        if (payment is not null)
        {
            payment.Amount = order.TotalAmount;
            payment.Currency = request.Currency;
            payment.Provider = request.Provider;
            payment.UpdatedTime = DateTime.UtcNow;
            return (payment, false);
        }

        payment = new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Currency = request.Currency,
            Provider = request.Provider,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };
        await _paymentRepository.AddAsync(payment, token);
        return (payment, true);
    }

    private static void DeterminePaymentResult(Order order, PaymentRequestDto request, Payment payment)
    {
        if (request.Amount != order.TotalAmount)
        {
            SetPaymentFailed(payment, "支付金额与订单金额不一致。");
            return;
        }

        var shouldFail = string.Equals(request.PaymentMethod, "simulate-failure", StringComparison.OrdinalIgnoreCase);
        if (shouldFail)
        {
            SetPaymentFailed(payment, "已根据请求模拟支付失败。");
            return;
        }

        SetPaymentSucceeded(payment, $"PAY-{Guid.NewGuid():N}");
    }

    private static PaymentResponseDto Map(Payment payment) =>
        new(payment.Id, payment.Status, payment.ProviderReference, payment.FailureReason);

    private static void SetPaymentSucceeded(Payment payment, string providerReference)
    {
        payment.Status = PaymentStatus.Succeeded;
        payment.ProviderReference = providerReference;
        payment.FailureReason = null;
        payment.UpdatedTime = DateTime.UtcNow;
    }

    private static void SetPaymentFailed(Payment payment, string reason)
    {
        payment.Status = PaymentStatus.Failed;
        payment.ProviderReference = null;
        payment.FailureReason = reason;
        payment.UpdatedTime = DateTime.UtcNow;
    }
}
