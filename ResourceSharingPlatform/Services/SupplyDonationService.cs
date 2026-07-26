using Microsoft.EntityFrameworkCore;
using ResourceSharingPlatform.Data;
using ResourceSharingPlatform.Models;
using ResourceSharingPlatform.Models.ViewModels;

namespace ResourceSharingPlatform.Services
{
    public class SupplyDonationService
    {
        private readonly ApplicationDbContext _context;

        public SupplyDonationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> DonateAsync(DonationViewModel model, string? operatorName)
        {
            if (model.DonationQuantity <= 0)
            {
                return (false, "捐贈數量必須大於 0");
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

                item.Quantity += model.DonationQuantity;
                item.UpdatedAt = DateTime.Now;

                var log = new SupplyDonationLog
                {
                    SupplyItemId = item.Id,
                    LocationId = item.LocationId,
                    DonationQuantity = model.DonationQuantity,
                    DonorName = model.DonorName,
                    DonorContact = model.DonorContact,
                    Operator = operatorName,
                    DonationTime = DateTime.Now,
                    Remark = model.Remark
                };

                _context.SupplyDonationLogs.Add(log);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"感謝 {model.DonorName} 的捐贈！已登記並更新庫存");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "捐贈登記失敗：" + ex.Message);
            }
        }
    }
}
