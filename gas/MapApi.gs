function MapApi_getLocations(payload, token) {
  requireAuth(token);
  var locations = sheetToObjects('Locations').filter(function (r) { return toBool(r.IsActive); });
  var items = sheetToObjects('Items').filter(function (r) { return toBool(r.IsActive); });

  var today = new Date(); today.setHours(0, 0, 0, 0);
  var soon = new Date(today.getTime() + 30 * 24 * 60 * 60 * 1000);

  return locations.map(function (loc) {
    var locItems = items.filter(function (i) { return String(i.LocationId) === String(loc.Id); });
    var lowStock = 0, expiring = 0, totalQty = 0;
    locItems.forEach(function (item) {
      totalQty += Number(item.Quantity) || 0;
      if (Number(item.Quantity) <= Number(item.SafetyStock)) lowStock++;
      if (item.ExpirationDate) {
        var exp = new Date(item.ExpirationDate);
        if (exp >= today && exp <= soon) expiring++;
      }
    });
    return {
      Id: loc.Id,
      LocationName: loc.LocationName,
      Address: loc.Address,
      Latitude: loc.Latitude,
      Longitude: loc.Longitude,
      ContactPerson: loc.ContactPerson,
      Phone: loc.Phone,
      ItemCount: locItems.length,
      TotalQuantity: totalQty,
      LowStockCount: lowStock,
      ExpiringSoonCount: expiring
    };
  });
}
