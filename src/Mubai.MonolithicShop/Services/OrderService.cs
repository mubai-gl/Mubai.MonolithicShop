using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Repositories;
using Mubai.Snowflake;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 订单服务，实现下单流程、库存协作与支付联动。
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IInventoryService inventoryService,
        IPaymentService paymentService,
        IIdGenerator idGenerator,
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _paymentService = paymentService;
        _idGenerator = idGenerator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OrderResponseDto> PlaceOrderAsync(PlaceOrderRequestDto request, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var user = await LoadUserAsync(request.UserId, token);
        var products = await LoadProductsAsync(request.Items, token);
        var order = BuildOrder(user.Id, request, products);

        await PersistNewOrderAsync(order, token);
        await ReserveInventoryAsync(order, request.Items, token);
        await UpdateOrderStatusAsync(order, OrderStatus.AwaitingPayment, token);

        await HandlePaymentAsync(order, request.Items, request.Payment!, token);

        return await GetAsync(order.Id, token) ?? Map(order);
    }

    public async Task<OrderResponseDto?> GetAsync(long orderId, CancellationToken token = default)
    {
        var order = await _orderRepository.GetWithItemsAsync(orderId, token);
        return order is null ? null : Map(order);
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

    private async Task<ApplicationUser> LoadUserAsync(Guid userId, CancellationToken token)
    {
        var user = await _unitOfWork.DbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, token);
        return user ?? throw new KeyNotFoundException("未找到对应用户，无法创建订单。");
    }

    private async Task<IReadOnlyCollection<Product>> LoadProductsAsync(IReadOnlyCollection<OrderItemRequestDto> items, CancellationToken token)
    {
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productRepository.Query()
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(token);

        if (products.Count != productIds.Count)
        {
            throw new InvalidOperationException("存在无法识别的商品，请检查订单明细。");
        }

        return products;
    }

    private Order BuildOrder(Guid userId, PlaceOrderRequestDto request, IReadOnlyCollection<Product> products)
    {
        var order = new Order
        {
            Id = _idGenerator.NewId(),
            UserId = userId,
            Notes = request.Notes
        };

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException("商品数量必须大于 0。");
            }

            var product = products.First(p => p.Id == item.ProductId);
            AddOrUpdateOrderItem(order, product, item.Quantity);
        }

        RefreshOrderTotals(order);
        return order;
    }

    private async Task PersistNewOrderAsync(Order order, CancellationToken token)
    {
        await _orderRepository.AddAsync(order, token);
        await _unitOfWork.SaveChangesAsync(token);
        _logger.LogInformation("已创建订单 {OrderId}，对应用户 {UserId}", order.Id, order.UserId);
    }

    private async Task ReserveInventoryAsync(Order order, IReadOnlyCollection<OrderItemRequestDto> items, CancellationToken token)
    {
        var inventoryResult = await _inventoryService.TryReserveStockAsync(items, token);
        if (inventoryResult.Success)
        {
            return;
        }

        await UpdateOrderStatusAsync(order, OrderStatus.InventoryFailed, token);
        var message = string.Join(";", inventoryResult.Errors);
        _logger.LogWarning("订单 {OrderId} 库存锁定失败: {Errors}", order.Id, message);
        throw new InvalidOperationException(message);
    }

    private async Task HandlePaymentAsync(
        Order order,
        IReadOnlyCollection<OrderItemRequestDto> items,
        PaymentRequestDto paymentRequest,
        CancellationToken token)
    {
        var paymentResult = await _paymentService.ProcessPaymentAsync(order, paymentRequest with { Amount = order.TotalAmount }, token);
        if (paymentResult.Status == PaymentStatus.Succeeded)
        {
            await OnPaymentSucceededAsync(order, items, token);
            return;
        }

        await OnPaymentFailedAsync(order, items, token);
    }

    private async Task OnPaymentSucceededAsync(Order order, IReadOnlyCollection<OrderItemRequestDto> items, CancellationToken token)
    {
        await _inventoryService.CommitReservationAsync(items, token);
        await UpdateOrderStatusAsync(order, OrderStatus.Paid, token);
        _logger.LogInformation("订单 {OrderId} 支付成功。", order.Id);
    }

    private async Task OnPaymentFailedAsync(Order order, IReadOnlyCollection<OrderItemRequestDto> items, CancellationToken token)
    {
        await _inventoryService.ReleaseReservationAsync(items, token);
        await UpdateOrderStatusAsync(order, OrderStatus.PaymentFailed, token);
        _logger.LogWarning("订单 {OrderId} 支付失败。", order.Id);
    }

    private async Task UpdateOrderStatusAsync(Order order, OrderStatus status, CancellationToken token)
    {
        order.Status = status;
        order.UpdatedTime = DateTime.UtcNow;
        order.ConcurrencyStamp++;

        _orderRepository.Update(order);
        await SaveOrderChangesAsync(token);
    }

    private static OrderResponseDto Map(Order order)
    {
        var items = order.Items.Select(i =>
            new OrderItemResponseDto(i.ProductId, i.Product?.Name ?? string.Empty, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList();

        return new OrderResponseDto(order.Id, order.Status, order.TotalAmount, items);
    }

    private static void AddOrUpdateOrderItem(Order order, Product product, int quantity)
    {
        var existing = order.Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is null)
        {
            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Product = product,
                Quantity = quantity,
                UnitPrice = product.Price,
                CreatedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow
            });
            return;
        }

        existing.Quantity += quantity;
        existing.UnitPrice = product.Price;
        existing.UpdatedTime = DateTime.UtcNow;
    }

    private static void RefreshOrderTotals(Order order)
    {
        order.TotalAmount = order.Items.Sum(i => i.TotalPrice);
        order.UpdatedTime = DateTime.UtcNow;
    }

    private async Task SaveOrderChangesAsync(CancellationToken token)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("订单状态已被其他请求修改，请刷新后重试。");
        }
    }
}
