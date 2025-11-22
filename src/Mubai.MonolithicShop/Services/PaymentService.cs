using Mubai.MonolithicShop.Dtos.Payment;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Repositories;
using Mubai.UnitOfWork.Abstractions;
using Mubai.UnitOfWork.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 支付服务，模拟支付网关并记录结果。
/// </summary>
public class PaymentService(
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    IInventoryRepository inventoryRepository,
    IUnitOfWork<ShopDbContext> unitOfWork,
    ILogger<PaymentService> logger) : IPaymentService
{
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> PaymentLocks = new();
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IInventoryRepository _inventoryRepository = inventoryRepository;
    private readonly IUnitOfWork<ShopDbContext> _unitOfWork = unitOfWork;
    private readonly ILogger<PaymentService> _logger = logger;

    /// <inheritdoc />
    public async Task ProcessPaymentAsync(ProcessPaymentRequestDto request, CancellationToken token = default)
    {
        var gate = PaymentLocks.GetOrAdd(request.OrderId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(token);
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var order = await _orderRepository.GetWithItemsAsync(request.OrderId, token)
                            ?? throw new InvalidOperationException("订单不存在，无法处理支付");
                if (order.Status == OrderStatus.Paid)
                {
                    return;
                }

                var (payment, isNew) = await LoadOrCreatePaymentAsync(request, token);
                if (!isNew && payment.Status == PaymentStatus.Succeeded)
                {
                    return;
                }

                DeterminePaymentResult(order, request, payment);
                if (!isNew)
                {
                    _paymentRepository.Update(payment);
                }

                if (payment.Status == PaymentStatus.Succeeded)
                {
                    await UpdateOrderStatusAsync(order, OrderStatus.Paid);
                    await CommitReservationAsync(order.Items, token);
                }
                else
                {
                    await UpdateOrderStatusAsync(order, OrderStatus.PaymentFailed);
                    await ReleaseReservationAsync(order.Items, token);
                }

                _orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync(token);
            }, token);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<(Payment payment, bool isNew)> LoadOrCreatePaymentAsync(ProcessPaymentRequestDto request, CancellationToken token)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(request.OrderId, token);
        if (payment is not null)
        {
            payment.Amount = request.Amount;
            payment.Provider = request.Provider;
            payment.UpdatedTime = DateTime.UtcNow;
            return (payment, false);
        }

        payment = new Payment
        {
            OrderId = request.OrderId,
            Amount = request.Amount,
            Provider = request.Provider,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };
        await _paymentRepository.AddAsync(payment, token);
        return (payment, true);
    }

    private static void DeterminePaymentResult(Order order, ProcessPaymentRequestDto request, Payment payment)
    {
        if (request.Amount != order.TotalAmount)
        {
            SetPaymentFailed(payment, "支付金额与订单总额不一致。");
            return;
        }

        var shouldFail = string.Equals(request.PaymentMethod, "simulate-failure", StringComparison.OrdinalIgnoreCase);
        if (shouldFail)
        {
            SetPaymentFailed(payment, "已按请求模拟支付失败。");
            return;
        }

        SetPaymentSucceeded(payment, $"PAY-{Guid.NewGuid():N}");
    }

    /// <summary>
    /// 将支付记录标记为成功，并写入网关参考号。
    /// </summary>
    private static void SetPaymentSucceeded(Payment payment, string providerReference)
    {
        payment.Status = PaymentStatus.Succeeded;
        payment.ProviderReference = providerReference;
        payment.FailureReason = null;
        payment.UpdatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 将支付记录标记为失败，并写入失败原因。
    /// </summary>
    private static void SetPaymentFailed(Payment payment, string reason)
    {
        payment.Status = PaymentStatus.Failed;
        payment.ProviderReference = null;
        payment.FailureReason = reason;
        payment.UpdatedTime = DateTime.UtcNow;
    }

    private async Task UpdateOrderStatusAsync(Order order, OrderStatus status)
    {
        order.Status = status;
        order.UpdatedTime = DateTime.UtcNow;
        order.ConcurrencyStamp++;
    }

    /// <summary>
    /// 释放预留库存。
    /// </summary>
    private async Task ReleaseReservationAsync(IEnumerable<OrderItem> items, CancellationToken token = default)
    {
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var entries = await _inventoryRepository.GetByProductIdsAsync(productIds, token);
        var inventoryMap = entries.ToDictionary(i => i.ProductId);
        List<Guid>? missingProducts = null;
        foreach (var item in items)
        {
            if (!inventoryMap.TryGetValue(item.ProductId, out var inventoryItem))
            {
                missingProducts ??= new List<Guid>();
                missingProducts.Add(item.ProductId);
                continue;
            }

            ReleaseReservation(inventoryItem, item.Quantity);
        }

        if (missingProducts is { Count: > 0 })
        {
            throw new InvalidOperationException($"部分商品不存在，无法释放预留库存: {string.Join(',', missingProducts)}");
        }
    }

    /// <summary>
    /// 提交预留库存。
    /// </summary>
    private async Task CommitReservationAsync(IEnumerable<OrderItem> items, CancellationToken token = default)
    {
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var entries = await _inventoryRepository.GetByProductIdsAsync(productIds, token);
        var inventoryMap = entries.ToDictionary(i => i.ProductId);

        List<Guid>? missingProducts = null;
        foreach (var item in items)
        {
            if (!inventoryMap.TryGetValue(item.ProductId, out var inventoryItem))
            {
                missingProducts ??= new List<Guid>();
                missingProducts.Add(item.ProductId);
                continue;
            }
            CommitReservation(inventoryItem, item.Quantity);
        }

        if (missingProducts is { Count: > 0 })
        {
            throw new InvalidOperationException($"部分商品不存在，无法扣减预留库存: {string.Join(',', missingProducts)}");
        }
    }

    private static void ReleaseReservation(InventoryItem inventoryItem, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        inventoryItem.ReservedQuantity = Math.Max(0, inventoryItem.ReservedQuantity - quantity);
        inventoryItem.UpdatedTime = DateTime.UtcNow;
        inventoryItem.ConcurrencyStamp++;
    }

    private static void CommitReservation(InventoryItem inventoryItem, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        if (inventoryItem.ReservedQuantity < quantity)
        {
            throw new InvalidOperationException("扣减数量超过已预留数量。");
        }

        inventoryItem.ReservedQuantity -= quantity;
        inventoryItem.QuantityOnHand -= quantity;
        inventoryItem.UpdatedTime = DateTime.UtcNow;
        inventoryItem.ConcurrencyStamp++;
    }

    /// <inheritdoc />
    public async Task<PaymentResponseDto?> GetByOrderAsync(long orderId, CancellationToken token = default)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(orderId, token);
        if (payment is not null)
        {
            return new(payment.Id, payment.Status, payment.ProviderReference, payment.FailureReason);
        }
        return null;
    }
}
