using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;

namespace ResourceSharingPlatform.Services
{
    public class SupplyTransferService
    {
        private readonly ApplicationDbContext _context;

        public SupplyTransferService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> CreateBatchAsync(TransferBatchViewModel model, string? operatorName)
        {
            if (model.FromLocationId == model.ToLocationId)
            {
                return (false, "來源據點與目標據點不可相同");
            }

            if (model.Lines == null || model.Lines.Count == 0)
            {
                return (false, "請至少選擇一項物資");
            }

            // Merge duplicate item selections within the same batch
            var mergedLines = model.Lines
                .GroupBy(x => x.SupplyItemId)
                .Select(g => new TransferLineViewModel { SupplyItemId = g.Key, TransferQuantity = g.Sum(x => x.TransferQuantity) })
                .ToList();

            foreach (var line in mergedLines)
            {
                if (line.TransferQuantity <= 0)
                {
                    return (false, "轉移數量必須大於 0");
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var batchId = Guid.NewGuid();
                var now = DateTime.Now;

                foreach (var line in mergedLines)
                {
                    var sourceItem = await _context.SupplyItems
                        .FirstOrDefaultAsync(x => x.Id == line.SupplyItemId && x.LocationId == model.FromLocationId && x.IsActive);

                    if (sourceItem == null)
                    {
                        return (false, $"找不到來源物資（Id={line.SupplyItemId}）");
                    }

                    if (sourceItem.Quantity < line.TransferQuantity)
                    {
                        return (false, $"「{sourceItem.ItemName}」來源數量不足，目前只有 {sourceItem.Quantity} {sourceItem.Unit}");
                    }

                    sourceItem.Quantity -= line.TransferQuantity;
                    sourceItem.UpdatedAt = now;

                    var log = new SupplyTransferLog
                    {
                        BatchId = batchId,
                        SupplyItemId = sourceItem.Id,
                        FromLocationId = model.FromLocationId,
                        ToLocationId = model.ToLocationId,
                        TransferQuantity = line.TransferQuantity,
                        TransferTime = now,
                        Status = TransferStatuses.Pending,
                        Operator = operatorName ?? model.Operator,
                        Remark = model.Remark
                    };

                    _context.SupplyTransferLogs.Add(log);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"轉移已建立，共 {mergedLines.Count} 項物資，待對方確認送達後才會計入目標據點庫存");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "轉移失敗：" + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> ConfirmAsync(int logId, string? confirmedBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var line = await _context.SupplyTransferLogs
                    .FirstOrDefaultAsync(x => x.Id == logId && x.Status == TransferStatuses.Pending);

                if (line == null)
                {
                    return (false, "找不到待確認的轉移紀錄，可能已經處理過");
                }

                var now = DateTime.Now;

                var sourceItem = await _context.SupplyItems.FindAsync(line.SupplyItemId);
                if (sourceItem == null)
                {
                    return (false, "找不到對應的物資資料");
                }

                var targetItem = await _context.SupplyItems
                    .FirstOrDefaultAsync(x =>
                        x.ItemName == sourceItem.ItemName &&
                        x.Category == sourceItem.Category &&
                        x.LocationId == line.ToLocationId &&
                        x.ExpirationDate == sourceItem.ExpirationDate &&
                        x.IsActive);

                if (targetItem == null)
                {
                    var destinationSafetyStock = 0;
                    if (sourceItem.InventoryItemVariantId.HasValue)
                    {
                        var definitionId = await _context.InventoryItemVariants
                            .Where(x => x.Id == sourceItem.InventoryItemVariantId.Value)
                            .Select(x => (int?)x.InventoryItemDefinitionId)
                            .FirstOrDefaultAsync();
                        if (definitionId.HasValue)
                        {
                            destinationSafetyStock = await _context.LocationInventorySafetyStocks
                                .Where(x => x.LocationId == line.ToLocationId && x.InventoryItemDefinitionId == definitionId.Value)
                                .Select(x => (int?)x.SafetyStock)
                                .FirstOrDefaultAsync() ?? 0;
                        }
                    }

                    targetItem = new SupplyItem
                    {
                        Category = sourceItem.Category,
                        ItemName = sourceItem.ItemName,
                        Specification = sourceItem.Specification,
                        Quantity = line.TransferQuantity,
                        Unit = sourceItem.Unit,
                        StockType = sourceItem.StockType,
                        ExpirationDate = sourceItem.ExpirationDate,
                        InventoryItemVariantId = sourceItem.InventoryItemVariantId,
                        LocationId = line.ToLocationId,
                        SafetyStock = destinationSafetyStock,
                        Remark = sourceItem.Remark,
                        IsActive = true,
                        CreatedAt = now
                    };

                    _context.SupplyItems.Add(targetItem);
                }
                else
                {
                    targetItem.Quantity += line.TransferQuantity;
                    targetItem.UpdatedAt = now;
                }

                line.Status = TransferStatuses.Confirmed;
                line.ConfirmedBy = confirmedBy;
                line.ConfirmedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"「{sourceItem.ItemName}」已確認送達，目標據點庫存已更新");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "確認失敗：" + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> CancelAsync(int logId, string? cancelledBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var line = await _context.SupplyTransferLogs
                    .FirstOrDefaultAsync(x => x.Id == logId && x.Status == TransferStatuses.Pending);

                if (line == null)
                {
                    return (false, "找不到待確認的轉移紀錄，可能已經處理過");
                }

                var now = DateTime.Now;

                var sourceItem = await _context.SupplyItems.FindAsync(line.SupplyItemId);
                if (sourceItem != null)
                {
                    sourceItem.Quantity += line.TransferQuantity;
                    sourceItem.UpdatedAt = now;
                }

                line.Status = TransferStatuses.Cancelled;
                line.ConfirmedBy = cancelledBy;
                line.ConfirmedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"「{sourceItem?.ItemName}」轉移已取消，來源據點庫存已退回");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "取消失敗：" + ex.Message);
            }
        }
    }
}
