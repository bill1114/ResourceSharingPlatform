using ResourceSharingPlatform.Models.ViewModels;
using ResourceSharingPlatform.Services.GoogleSheets;

namespace ResourceSharingPlatform.Services
{
    public class SupplyTransferService
    {
        private readonly SheetsDataStore _store;

        public SupplyTransferService(SheetsDataStore store)
        {
            _store = store;
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

            // The actual read-check-write against the sheet runs atomically inside
            // the Apps Script Web App (under a script lock), replacing the EF Core
            // transaction that used to guard this.
            return await _store.CreateTransferBatchAsync(
                model.FromLocationId,
                model.ToLocationId,
                mergedLines.Select(l => (l.SupplyItemId, l.TransferQuantity)),
                operatorName ?? model.Operator,
                model.Remark);
        }

        public Task<(bool Success, string Message)> ConfirmAsync(int logId, string? confirmedBy)
            => _store.ConfirmTransferAsync(logId, confirmedBy);

        public Task<(bool Success, string Message)> CancelAsync(int logId, string? cancelledBy)
            => _store.CancelTransferAsync(logId, cancelledBy);
    }
}
