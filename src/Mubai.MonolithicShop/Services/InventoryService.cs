using Microsoft.EntityFrameworkCore;
using Mubai.MonolithicShop.Dtos;
using Mubai.MonolithicShop.Entities;
using Mubai.MonolithicShop.Infrastructure;
using Mubai.MonolithicShop.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mubai.MonolithicShop.Services;

/// <summary>
/// 库存服务，负责查询、调整及预留处理。
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<InventoryItemResponseDto>> GetInventoryAsync(CancellationToken token = default)
    {
        return await _inventoryRepository
            .Query()
            .AsNoTracking()
            .Select(i => new InventoryItemResponseDto(
                i.ProductId,
                i.Product != null ? i.Product.Name : string.Empty,
                i.QuantityOnHand,
                i.ReservedQuantity))
            .ToListAsync(token);
    }

    public async Task<InventoryItemResponseDto> AdjustInventoryAsync(AdjustInventoryRequestDto request, CancellationToken token = default)
    {
        var inventoryItem = await EnsureInventoryItemAsync(request.ProductId, token);
        ApplyStockAdjustment(inventoryItem, request.QuantityDelta);
        await _unitOfWork.SaveChangesAsync(token);

        return Map(inventoryItem);
    }

    public async Task<InventoryReservationResultDto> TryReserveStockAsync(IEnumerable<OrderItemRequestDto> items, CancellationToken token = default)
    {
        var itemList = MaterializeItems(items);
        if (itemList.Count == 0)
        {
            return new InventoryReservationResultDto(false, new[] { "订单必须包含明细才能锁定库存。" });
        }

        var inventoryMap = await LoadInventoryMapAsync(itemList.Select(i => i.ProductId), token);
        var errors = ValidateReservationRequests(itemList, inventoryMap);
        if (errors.Count > 0)
        {
            return new InventoryReservationResultDto(false, errors);
        }

        foreach (var item in itemList)
        {
            var inventoryItem = inventoryMap[item.ProductId];
            ReserveStock(inventoryItem, item.Quantity);
        }

        await _unitOfWork.SaveChangesAsync(token);
        return new InventoryReservationResultDto(true, Array.Empty<string>());
    }

    public Task ReleaseReservationAsync(IEnumerable<OrderItemRequestDto> items, CancellationToken token = default) =>
        ProcessReservationItemsAsync(items, token, ReleaseReservation);

    public Task CommitReservationAsync(IEnumerable<OrderItemRequestDto> items, CancellationToken token = default) =>
        ProcessReservationItemsAsync(items, token, CommitReservation);

    private async Task ProcessReservationItemsAsync(
        IEnumerable<OrderItemRequestDto> items,
        CancellationToken token,
        Action<InventoryItem, int> processor)
    {
        ArgumentNullException.ThrowIfNull(processor);

        var itemList = MaterializeItems(items);
        if (itemList.Count == 0)
        {
            return;
        }

        var inventoryMap = await LoadInventoryMapAsync(itemList.Select(i => i.ProductId), token);
        foreach (var item in itemList)
        {
            if (inventoryMap.TryGetValue(item.ProductId, out var inventoryItem))
            {
                processor(inventoryItem, item.Quantity);
            }
        }

        await _unitOfWork.SaveChangesAsync(token);
    }

    private async Task<InventoryItem> EnsureInventoryItemAsync(Guid productId, CancellationToken token)
    {
        var inventoryItem = await _inventoryRepository.GetByProductIdAsync(productId, token);
        if (inventoryItem is not null)
        {
            return inventoryItem;
        }

        var product = await _productRepository.GetByIdAsync(productId, token)
                      ?? throw new KeyNotFoundException("商品不存在，无法建立库存。");

        inventoryItem = CreateInventoryItem(product);
        await _inventoryRepository.AddAsync(inventoryItem, token);
        await _unitOfWork.SaveChangesAsync(token);

        return inventoryItem;
    }

    private static InventoryItem CreateInventoryItem(Product product) =>
        new()
        {
            ProductId = product.Id,
            Product = product,
            QuantityOnHand = 0,
            ReservedQuantity = 0,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

    private async Task<Dictionary<Guid, InventoryItem>> LoadInventoryMapAsync(IEnumerable<Guid> productIds, CancellationToken token)
    {
        var entries = await _inventoryRepository.GetByProductIdsAsync(productIds, token);
        return entries.ToDictionary(i => i.ProductId);
    }

    private static List<OrderItemRequestDto> MaterializeItems(IEnumerable<OrderItemRequestDto> items) =>
        items?.ToList() ?? new List<OrderItemRequestDto>();

    private static List<string> ValidateReservationRequests(
        IEnumerable<OrderItemRequestDto> items,
        IReadOnlyDictionary<Guid, InventoryItem> inventoryMap)
    {
        var errors = new List<string>();

        foreach (var item in items)
        {
            if (!inventoryMap.TryGetValue(item.ProductId, out var inventoryItem))
            {
                errors.Add($"未找到商品 {item.ProductId} 的库存记录。");
                continue;
            }

            if (inventoryItem.AvailableQuantity < item.Quantity)
            {
                var productName = inventoryItem.Product?.Name ?? item.ProductId.ToString();
                errors.Add($"商品 {productName} 库存不足。");
            }
        }

        return errors;
    }

    private static InventoryItemResponseDto Map(InventoryItem inventoryItem) =>
        new(inventoryItem.ProductId, inventoryItem.Product?.Name ?? string.Empty, inventoryItem.QuantityOnHand, inventoryItem.ReservedQuantity);

    private static void ApplyStockAdjustment(InventoryItem inventoryItem, int quantityDelta)
    {
        var newQuantity = inventoryItem.QuantityOnHand + quantityDelta;
        if (newQuantity < 0)
        {
            throw new InvalidOperationException("库存不足，无法扣减。");
        }

        inventoryItem.QuantityOnHand = newQuantity;
        inventoryItem.UpdatedTime = DateTime.UtcNow;
    }

    private static void ReserveStock(InventoryItem inventoryItem, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "预留数量必须大于 0。");
        }

        if (inventoryItem.AvailableQuantity < quantity)
        {
            throw new InvalidOperationException("库存不足，无法预留。");
        }

        inventoryItem.ReservedQuantity += quantity;
        inventoryItem.UpdatedTime = DateTime.UtcNow;
    }

    private static void ReleaseReservation(InventoryItem inventoryItem, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        inventoryItem.ReservedQuantity = Math.Max(0, inventoryItem.ReservedQuantity - quantity);
        inventoryItem.UpdatedTime = DateTime.UtcNow;
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
    }
}
