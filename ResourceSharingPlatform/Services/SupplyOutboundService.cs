using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;

namespace ResourceSharingPlatform.Services
{
    public class SupplyOutboundService
    {
        private readonly ApplicationDbContext _context;

        public SupplyOutboundService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> IssueAsync(OutboundViewModel model, string? operatorName)
        {
            if (model.OutboundQuantity <= 0)
            {
                return (false, "出庫數量必須大於 0");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var item = await _context.SupplyItems
                    .FirstOrDefaultAsync(x => x.Id == model.SupplyItemId && x.LocationId == model.LocationId && x.IsActive);

                if (item == null)
                {
                    return (false, "找不到指定據點的這項物資");
                }

                if (item.Quantity < model.OutboundQuantity)
                {
                    return (false, $"庫存數量不足，目前僅有 {item.Quantity} {item.Unit}");
                }

                item.Quantity -= model.OutboundQuantity;
                item.UpdatedAt = DateTime.Now;

                var log = new SupplyOutboundLog
                {
                    SupplyItemId = item.Id,
                    LocationId = item.LocationId,
                    OutboundQuantity = model.OutboundQuantity,
                    RecipientName = model.RecipientName,
                    RecipientContact = model.RecipientContact,
                    Operator = operatorName,
                    OutboundTime = DateTime.Now,
                    Remark = model.Remark
                };

                _context.SupplyOutboundLogs.Add(log);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "出庫完成");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "出庫失敗：" + ex.Message);
            }
        }
    }
}
