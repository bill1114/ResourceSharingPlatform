using ResourceSharingPlatform.Models;

namespace ResourceSharingPlatform.Services.GoogleSheets
{
    // Replaces ApplicationDbContext. Reads go through the GAS Web App and then
    // attach navigation properties (Location, SupplyItem, ...) in memory, the
    // same way DashboardService/MapController already fetch full lists and
    // correlate them with LINQ instead of relying on SQL joins.
    public class SheetsDataStore
    {
        private readonly GoogleSheetsClient _client;

        // Per-request memoization (this class is registered Scoped) to avoid
        // re-fetching the same sheet multiple times while handling one request.
        private Task<List<SupplyLocation>>? _locationsCache;
        private Task<List<SupplyItem>>? _itemsCache;

        public SheetsDataStore(GoogleSheetsClient client)
        {
            _client = client;
        }

        // ---------------------------------------------------------------
        // Locations
        // ---------------------------------------------------------------

        public Task<List<SupplyLocation>> GetLocationsAsync()
        {
            return _locationsCache ??= FetchLocationsAsync();
        }

        private async Task<List<SupplyLocation>> FetchLocationsAsync()
        {
            var res = await _client.PostAsync<List<SupplyLocation>>("getLocations");
            return res.Data ?? new List<SupplyLocation>();
        }

        public async Task<SupplyLocation?> GetLocationByIdAsync(int id)
            => (await GetLocationsAsync()).FirstOrDefault(x => x.Id == id);

        public async Task<SupplyLocation> CreateLocationAsync(SupplyLocation location)
        {
            var res = await _client.PostAsync<SupplyLocation>("createLocation", new { location });
            return res.Data ?? location;
        }

        public async Task UpdateLocationAsync(SupplyLocation location)
        {
            await _client.PostAsync<SupplyLocation>("updateLocation", new { location });
        }

        // ---------------------------------------------------------------
        // Items
        // ---------------------------------------------------------------

        public Task<List<SupplyItem>> GetItemsAsync()
        {
            return _itemsCache ??= FetchItemsAsync();
        }

        private async Task<List<SupplyItem>> FetchItemsAsync()
        {
            var itemsTask = _client.PostAsync<List<SupplyItem>>("getItems");
            var locationsTask = GetLocationsAsync();
            await Task.WhenAll(itemsTask, locationsTask);

            var items = itemsTask.Result.Data ?? new List<SupplyItem>();
            var locationById = locationsTask.Result.ToDictionary(x => x.Id);

            foreach (var item in items)
            {
                locationById.TryGetValue(item.LocationId, out var location);
                item.Location = location;
            }

            return items;
        }

        public async Task<SupplyItem?> GetItemByIdAsync(int id)
            => (await GetItemsAsync()).FirstOrDefault(x => x.Id == id);

        public async Task<SupplyItem> CreateItemAsync(SupplyItem item)
        {
            var res = await _client.PostAsync<SupplyItem>("createItem", new { item });
            return res.Data ?? item;
        }

        public async Task UpdateItemAsync(SupplyItem item)
        {
            await _client.PostAsync<SupplyItem>("updateItem", new { item });
        }

        // ---------------------------------------------------------------
        // Users
        // ---------------------------------------------------------------

        public async Task<List<UserAccount>> GetUsersAsync()
        {
            var usersTask = _client.PostAsync<List<UserAccount>>("getUsers");
            var locationsTask = GetLocationsAsync();
            await Task.WhenAll(usersTask, locationsTask);

            var users = usersTask.Result.Data ?? new List<UserAccount>();
            var locationById = locationsTask.Result.ToDictionary(x => x.Id);

            foreach (var user in users)
            {
                if (user.LocationId.HasValue && locationById.TryGetValue(user.LocationId.Value, out var location))
                {
                    user.Location = location;
                }
            }

            return users;
        }

        public async Task<UserAccount?> GetUserByIdAsync(int id)
            => (await GetUsersAsync()).FirstOrDefault(x => x.Id == id);

        public async Task<UserAccount?> GetUserByUsernameAsync(string userName)
        {
            var res = await _client.PostAsync<UserAccount>("getUserByUsername", new { userName });
            return res.Data;
        }

        public async Task<UserAccount> CreateUserAsync(UserAccount user)
        {
            var res = await _client.PostAsync<UserAccount>("createUser", new { user });
            return res.Data ?? user;
        }

        public async Task UpdateUserAsync(UserAccount user)
        {
            await _client.PostAsync<UserAccount>("updateUser", new { user });
        }

        // ---------------------------------------------------------------
        // Transfer logs
        // ---------------------------------------------------------------

        public async Task<List<SupplyTransferLog>> GetTransferLogsAsync()
        {
            var logsTask = _client.PostAsync<List<SupplyTransferLog>>("getTransferLogs");
            var itemsTask = GetItemsAsync();
            var locationsTask = GetLocationsAsync();
            await Task.WhenAll(logsTask, itemsTask, locationsTask);

            var logs = logsTask.Result.Data ?? new List<SupplyTransferLog>();
            var itemById = itemsTask.Result.ToDictionary(x => x.Id);
            var locationById = locationsTask.Result.ToDictionary(x => x.Id);

            foreach (var log in logs)
            {
                itemById.TryGetValue(log.SupplyItemId, out var item);
                log.SupplyItem = item;
                locationById.TryGetValue(log.FromLocationId, out var from);
                log.FromLocation = from;
                locationById.TryGetValue(log.ToLocationId, out var to);
                log.ToLocation = to;
            }

            return logs;
        }

        public async Task<SupplyTransferLog?> GetTransferLogByIdAsync(int id)
            => (await GetTransferLogsAsync()).FirstOrDefault(x => x.Id == id);

        // ---------------------------------------------------------------
        // Outbound logs
        // ---------------------------------------------------------------

        public async Task<List<SupplyOutboundLog>> GetOutboundLogsAsync()
        {
            var logsTask = _client.PostAsync<List<SupplyOutboundLog>>("getOutboundLogs");
            var itemsTask = GetItemsAsync();
            var locationsTask = GetLocationsAsync();
            await Task.WhenAll(logsTask, itemsTask, locationsTask);

            var logs = logsTask.Result.Data ?? new List<SupplyOutboundLog>();
            var itemById = itemsTask.Result.ToDictionary(x => x.Id);
            var locationById = locationsTask.Result.ToDictionary(x => x.Id);

            foreach (var log in logs)
            {
                itemById.TryGetValue(log.SupplyItemId, out var item);
                log.SupplyItem = item;
                locationById.TryGetValue(log.LocationId, out var location);
                log.Location = location;
            }

            return logs;
        }

        // ---------------------------------------------------------------
        // Atomic business operations (executed server-side in Apps Script,
        // under a script lock, mirroring the EF Core transactions this
        // replaces)
        // ---------------------------------------------------------------

        public async Task<(bool Success, string Message)> CreateTransferBatchAsync(
            int fromLocationId,
            int toLocationId,
            IEnumerable<(int SupplyItemId, int TransferQuantity)> lines,
            string? operatorName,
            string? remark)
        {
            var payload = new
            {
                fromLocationId,
                toLocationId,
                lines = lines.Select(l => new { supplyItemId = l.SupplyItemId, transferQuantity = l.TransferQuantity }),
                operatorName,
                remark
            };

            var res = await _client.PostActionAsync("createTransferBatch", payload);
            return (res.Success, res.Message ?? string.Empty);
        }

        public async Task<(bool Success, string Message)> ConfirmTransferAsync(int logId, string? confirmedBy)
        {
            var res = await _client.PostActionAsync("confirmTransfer", new { logId, confirmedBy });
            return (res.Success, res.Message ?? string.Empty);
        }

        public async Task<(bool Success, string Message)> CancelTransferAsync(int logId, string? cancelledBy)
        {
            var res = await _client.PostActionAsync("cancelTransfer", new { logId, cancelledBy });
            return (res.Success, res.Message ?? string.Empty);
        }

        public async Task<(bool Success, string Message)> IssueOutboundAsync(
            int supplyItemId,
            int outboundQuantity,
            string recipientName,
            string? recipientContact,
            string? operatorName,
            string? remark)
        {
            var payload = new { supplyItemId, outboundQuantity, recipientName, recipientContact, operatorName, remark };
            var res = await _client.PostActionAsync("issueOutbound", payload);
            return (res.Success, res.Message ?? string.Empty);
        }
    }
}
