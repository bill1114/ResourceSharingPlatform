using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;

namespace ResourceSharingPlatform.Services
{
    public class SupplyDisposalService
    {
        private readonly ApplicationDbContext _context;

        public SupplyDisposalService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> DisposeAsync(DisposalViewModel model, string? operatorName)
        {
            if (model.DisposalQuantity <= 0)
            {
                return (false, "報廢數量必須大於 0");
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

                if (item.Quantity < model.DisposalQuantity)
                {
                    return (false, $"庫存數量不足，目前僅有 {item.Quantity} {item.Unit}");
                }

                item.Quantity -= model.DisposalQuantity;
                item.UpdatedAt = DateTime.Now;

                var log = new SupplyDisposalLog
                {
                    SupplyItemId = item.Id,
                    LocationId = item.LocationId,
                    DisposalQuantity = model.DisposalQuantity,
                    Reason = model.Reason,
                    Operator = operatorName,
                    DisposalTime = DateTime.Now,
                    Remark = model.Remark
                };

                _context.SupplyDisposalLogs.Add(log);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "報廢登記完成");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "報廢登記失敗：" + ex.Message);
            }
        }
    }
}
