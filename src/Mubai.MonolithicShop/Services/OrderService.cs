using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Dtos.Order;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Repositories;
using Mubai.Snowflake;
using Mubai.UnitOfWork.Abstractions;
using Mubai.UnitOfWork.EntityFrameworkCore;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 订单服务。
/// </summary>
public class OrderService(
    IOrderRepository orderRepository, 
    IInventoryRepository inventoryRepository, 
    IUnitOfWork<ShopDbContext> unitOfWork, 
    IIdGenerator idGenerator, 
    ILogger<OrderService> logger) : IOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IInventoryRepository _inventoryRepository = inventoryRepository;
    private readonly IUnitOfWork<ShopDbContext> _unitOfWork = unitOfWork;
    private readonly IIdGenerator _idGenerator = idGenerator;
    private readonly ILogger<OrderService> _logger = logger;

    /// <inheritdoc />
    public async Task PlaceOrderAsync(PlaceOrderRequestDto request, CancellationToken token = default)
    {
        ValidateRequest(request);
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = BuildOrder(request);
            await _orderRepository.AddAsync(order, token);

            await TryReserveInventoryAsync(order, request.Items, token);
            await UpdateOrderStatusAsync(order, OrderStatus.AwaitingPayment);
            await _unitOfWork.SaveChangesAsync(token);
        }, token);
    }

    private static void ValidateRequest(PlaceOrderRequestDto request)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new InvalidOperationException("订单必须包含至少一条商品明细。");
        }

        if (request.Payment is null)
        {
            throw new InvalidOperationException("支付信息不能为空。");
        }
    }

    private Order BuildOrder(PlaceOrderRequestDto request)
    {
        var order = new Order
        {
            Id = _idGenerator.NewId(),
            UserId = request.UserId,
            Notes = request.Notes
        };

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException("商品数量必须大于 0。");
            }

            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                CreatedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.TotalPrice);
        order.UpdatedTime = DateTime.UtcNow;
        return order;
    }

    #region 锁库存
    private async Task TryReserveInventoryAsync(Order order, IReadOnlyCollection<PlaceOrderItem> items, CancellationToken token = default)
    {
        try
        {
            var productIds = items.Select(i => i.ProductId).Distinct().ToList();
            var entries = await _inventoryRepository.GetByProductIdsAsync(productIds, token);
            var inventoryMap = entries.ToDictionary(e => e.ProductId);

            List<Guid>? missingProducts = null;
            foreach (var item in items)
            {
                if (!inventoryMap.TryGetValue(item.ProductId, out var inventoryItem))
                {
                    missingProducts ??= new List<Guid>();
                    missingProducts.Add(item.ProductId);
                    continue;
                }

                ReserveStock(inventoryItem, item.Quantity);
            }

            if (missingProducts is { Count: > 0 })
            {
                throw new InvalidOperationException($"以下商品不存在，无法预留库存: {string.Join(',', missingProducts)}");
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("预留库存时发生并发冲突");
        }
        catch (Exception)
        {
            await UpdateOrderStatusAsync(order, OrderStatus.InventoryFailed);
        }
    }
    private static void ReserveStock(InventoryItem inventoryItem, int quantity)
    {
        if (inventoryItem.AvailableQuantity < quantity)
        {
            throw new InvalidOperationException("库存不足，无法预留。");
        }

        inventoryItem.ReservedQuantity += quantity;
        inventoryItem.UpdatedTime = DateTime.UtcNow;
        inventoryItem.ConcurrencyStamp++;
    }
    #endregion

    private async Task UpdateOrderStatusAsync(Order order, OrderStatus status)
    {
        order.Status = status;
        order.UpdatedTime = DateTime.UtcNow;
        order.ConcurrencyStamp++;
    }

    public async Task<OrderResponseDto?> GetAsync(long orderId, CancellationToken token = default)
    {
        var order = await _orderRepository.GetWithItemsAsync(orderId, token);
        if (order is not null)
        {
            var items = order.Items.Select(i => new OrderItemResponseDto(
                i.ProductId,
                i.Product?.Name ?? string.Empty,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice)).ToList();

            return new OrderResponseDto(order.Id, order.Status, order.TotalAmount, items);
        }
        return null;
    }
}
