function Outbound_list(payload, token) {
  requireAuth(token);
  var logs = sheetToObjects('OutboundLogs');
  logs.sort(function (a, b) { return new Date(b.OutboundTime) - new Date(a.OutboundTime); });
  logs = logs.slice(0, 100);

  var items = sheetToObjects('Items');
  var locations = sheetToObjects('Locations');
  var itemMap = {}; items.forEach(function (i) { itemMap[i.Id] = i; });
  var locMap = {}; locations.forEach(function (l) { locMap[l.Id] = l; });

  logs.forEach(function (log) {
    delete log.__row;
    var item = itemMap[log.SupplyItemId];
    log.ItemName = item ? item.ItemName : '';
    log.Category = item ? item.Category : '';
    var loc = locMap[log.LocationId];
    log.LocationName = loc ? loc.LocationName : '';
  });

  var today = new Date(); today.setHours(0, 0, 0, 0);
  var soon = new Date(today.getTime() + 30 * 24 * 60 * 60 * 1000);
  var expiringItems = items.filter(function (i) {
    if (!toBool(i.IsActive) || !i.ExpirationDate) return false;
    var exp = new Date(i.ExpirationDate);
    return exp >= today && exp <= soon;
  }).map(function (i) {
    var loc = locMap[i.LocationId];
    return { Id: i.Id, ItemName: i.ItemName, Quantity: i.Quantity, ExpirationDate: i.ExpirationDate, LocationName: loc ? loc.LocationName : '' };
  }).slice(0, 20);

  return { logs: logs, expiringItems: expiringItems };
}

function Outbound_create(payload, token) {
  var user = requireAuth(token);
  var qty = Number(payload.outboundQuantity);
  if (!payload.supplyItemId) throw new Error('請選擇物資');
  if (!qty || qty <= 0) throw new Error('出庫數量必須大於 0');
  if (!payload.recipientName || !String(payload.recipientName).trim()) throw new Error('請輸入領取人姓名');

  return withLock(function () {
    var item = findById('Items', payload.supplyItemId);
    if (!item || !toBool(item.IsActive)) throw new Error('物資不存在或已停用');
    if (Number(item.Quantity) < qty) throw new Error('庫存不足，目前庫存：' + item.Quantity);

    updateObjectByRow('Items', item.__row, { Quantity: Number(item.Quantity) - qty, UpdatedAt: nowIso() });

    var log = {
      Id: nextId('OutboundLogs'),
      SupplyItemId: item.Id,
      LocationId: item.LocationId,
      OutboundQuantity: qty,
      RecipientName: payload.recipientName,
      RecipientContact: payload.recipientContact || '',
      Operator: user.displayName || user.userName,
      OutboundTime: nowIso(),
      Remark: payload.remark || ''
    };
    appendObject('OutboundLogs', log);
    return log;
  });
}
