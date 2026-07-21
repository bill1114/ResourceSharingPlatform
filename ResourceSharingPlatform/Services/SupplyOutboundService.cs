using ResourceSharingPlatform.Models.ViewModels;
using ResourceSharingPlatform.Services.GoogleSheets;

namespace ResourceSharingPlatform.Services
{
    public class SupplyOutboundService
    {
        private readonly SheetsDataStore _store;

        public SupplyOutboundService(SheetsDataStore store)
        {
            _store = store;
        }

        public async Task<(bool Success, string Message)> IssueAsync(OutboundViewModel model, string? operatorName)
        {
            if (model.OutboundQuantity <= 0)
            {
                return (false, "出庫數量必須大於 0");
            }

            return await _store.IssueOutboundAsync(
                model.SupplyItemId,
                model.OutboundQuantity,
                model.RecipientName,
                model.RecipientContact,
                operatorName,
                model.Remark);
        }
    }
}
