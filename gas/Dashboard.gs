function Dashboard_get(payload, token) {
  requireAuth(token);
  var locations = sheetToObjects('Locations').filter(function (r) { return toBool(r.IsActive); });
  var items = sheetToObjects('Items').filter(function (r) { return toBool(r.IsActive); });

  var today = new Date(); today.setHours(0, 0, 0, 0);
  var soon = new Date(today.getTime() + 30 * 24 * 60 * 60 * 1000);

  var totalItems = items.length;
  var totalQuantity = 0;
  var lowStockCount = 0, expiringSoonCount = 0, expiredCount = 0;
  var byLocation = {};

  locations.forEach(function (loc) {
    byLocation[loc.Id] = {
      LocationId: loc.Id,
      LocationName: loc.LocationName,
      ItemCount: 0,
      TotalQuantity: 0,
      LowStockCount: 0,
      ExpiringSoonCount: 0,
      ExpiredCount: 0
    };
  });

  items.forEach(function (item) {
    totalQuantity += Number(item.Quantity) || 0;
    var isLow = Number(item.Quantity) <= Number(item.SafetyStock);
    var isExpired = false, isExpiringSoon = false;
    if (item.ExpirationDate) {
      var exp = new Date(item.ExpirationDate);
      if (exp < today) isExpired = true;
      else if (exp <= soon) isExpiringSoon = true;
    }
    if (isLow) lowStockCount++;
    if (isExpired) expiredCount++;
    if (isExpiringSoon) expiringSoonCount++;

    var bucket = byLocation[item.LocationId];
    if (bucket) {
      bucket.ItemCount++;
      bucket.TotalQuantity += Number(item.Quantity) || 0;
      if (isLow) bucket.LowStockCount++;
      if (isExpired) bucket.ExpiredCount++;
      if (isExpiringSoon) bucket.ExpiringSoonCount++;
    }
  });

  return {
    TotalLocations: locations.length,
    TotalItems: totalItems,
    TotalQuantity: totalQuantity,
    LowStockCount: lowStockCount,
    ExpiringSoonCount: expiringSoonCount,
    ExpiredCount: expiredCount,
    LocationSummaries: Object.keys(byLocation).map(function (k) { return byLocation[k]; })
  };
}
