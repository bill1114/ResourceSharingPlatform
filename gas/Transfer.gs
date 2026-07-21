function Transfer_list(payload, token) {
  requireAuth(token);
  var logs = sheetToObjects('TransferLogs');
  logs.sort(function (a, b) { return new Date(b.TransferTime) - new Date(a.TransferTime); });
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
    var fromLoc = locMap[log.FromLocationId];
    var toLoc = locMap[log.ToLocationId];
    log.FromLocationName = fromLoc ? fromLoc.LocationName : '';
    log.ToLocationName = toLoc ? toLoc.LocationName : '';
  });
  return logs;
}

function canResolveTransfer(user, toLocationId) {
  if (user.role === Roles.Admin) return true;
  if (!user.locationId) return false;
  return Number(user.locationId) === Number(toLocationId);
}

/** payload: { fromLocationId, toLocationId, remark, lines: [{ itemId, quantity }] } */
function Transfer_createBatch(payload, token) {
  var user = requireAuth(token);
  requireRole(user, [Roles.Admin, Roles.Cadre]);

  var fromId = Number(payload.fromLocationId);
  var toId = Number(payload.toLocationId);
  if (!fromId || !toId) throw new Error('請選擇轉出與轉入據點');
  if (fromId === toId) throw new Error('轉出與轉入據點不可相同');
  if (!payload.lines || !payload.lines.length) throw new Error('請至少新增一筆物資');

  var fromLoc = findById('Locations', fromId);
  var toLoc = findById('Locations', toId);
  if (!fromLoc || !toBool(fromLoc.IsActive)) throw new Error('轉出據點不存在或已停用');
  if (!toLoc || !toBool(toLoc.IsActive)) throw new Error('轉入據點不存在或已停用');

  // merge duplicate item lines
  var merged = {};
  payload.lines.forEach(function (line) {
    if (!line.itemId || !line.quantity || Number(line.quantity) <= 0) throw new Error('每筆物資的數量必須大於 0');
    var key = String(line.itemId);
    merged[key] = (merged[key] || 0) + Number(line.quantity);
  });

  return withLock(function () {
    var createdLogs = [];
    var batchId = Utilities.getUuid();

    Object.keys(merged).forEach(function (itemId) {
      var qty = merged[itemId];
      var item = findById('Items', itemId);
      if (!item || !toBool(item.IsActive)) throw new Error('物資不存在或已停用（Id=' + itemId + '）');
      if (Number(item.LocationId) !== fromId) throw new Error('物資「' + item.ItemName + '」不屬於轉出據點');
      if (Number(item.Quantity) < qty) throw new Error('物資「' + item.ItemName + '」庫存不足，目前庫存：' + item.Quantity);

      updateObjectByRow('Items', item.__row, { Quantity: Number(item.Quantity) - qty, UpdatedAt: nowIso() });

      var log = {
        Id: nextId('TransferLogs'),
        BatchId: batchId,
        SupplyItemId: item.Id,
        FromLocationId: fromId,
        ToLocationId: toId,
        TransferQuantity: qty,
        TransferTime: nowIso(),
        Status: 'Pending',
        ConfirmedBy: '',
        ConfirmedAt: '',
        Operator: user.displayName || user.userName,
        Remark: payload.remark || ''
      };
      appendObject('TransferLogs', log);
      createdLogs.push(log);
    });

    return createdLogs;
  });
}

function Transfer_confirm(payload, token) {
  var user = requireAuth(token);
  if (!payload.logId) throw new Error('缺少 logId');

  return withLock(function () {
    var log = findById('TransferLogs', payload.logId);
    if (!log) throw new Error('找不到轉移紀錄');
    if (log.Status !== 'Pending') throw new Error('此筆轉移已處理，無法重複確認');
    if (!canResolveTransfer(user, log.ToLocationId)) throw new Error('權限不足：僅目的地據點的人員可以確認此筆轉移');

    var srcItem = findById('Items', log.SupplyItemId);
    if (!srcItem) throw new Error('找不到來源物資資料');

    var destItems = sheetToObjects('Items').filter(function (i) {
      return toBool(i.IsActive) &&
        String(i.LocationId) === String(log.ToLocationId) &&
        i.ItemName === srcItem.ItemName &&
        i.Category === srcItem.Category &&
        String(i.ExpirationDate || '') === String(srcItem.ExpirationDate || '');
    });

    if (destItems.length > 0) {
      var dest = destItems[0];
      updateObjectByRow('Items', dest.__row, { Quantity: Number(dest.Quantity) + Number(log.TransferQuantity), UpdatedAt: nowIso() });
    } else {
      appendObject('Items', {
        Id: nextId('Items'),
        Category: srcItem.Category,
        ItemName: srcItem.ItemName,
        Specification: srcItem.Specification || '',
        Quantity: Number(log.TransferQuantity),
        Unit: srcItem.Unit || '',
        StockType: srcItem.StockType || 'NoExpiry',
        ExpirationDate: srcItem.ExpirationDate || '',
        ImagePath: srcItem.ImagePath || '',
        LocationId: Number(log.ToLocationId),
        SafetyStock: srcItem.SafetyStock || 0,
        Remark: srcItem.Remark || '',
        IsActive: true,
        CreatedAt: nowIso(),
        UpdatedAt: ''
      });
    }

    return updateObjectByRow('TransferLogs', log.__row, {
      Status: 'Confirmed',
      ConfirmedBy: user.displayName || user.userName,
      ConfirmedAt: nowIso()
    });
  });
}

function Transfer_cancel(payload, token) {
  var user = requireAuth(token);
  if (!payload.logId) throw new Error('缺少 logId');

  return withLock(function () {
    var log = findById('TransferLogs', payload.logId);
    if (!log) throw new Error('找不到轉移紀錄');
    if (log.Status !== 'Pending') throw new Error('此筆轉移已處理，無法取消');
    if (!canResolveTransfer(user, log.ToLocationId)) throw new Error('權限不足：僅目的地據點的人員可以取消此筆轉移');

    var srcItem = findById('Items', log.SupplyItemId);
    if (srcItem) {
      updateObjectByRow('Items', srcItem.__row, { Quantity: Number(srcItem.Quantity) + Number(log.TransferQuantity), UpdatedAt: nowIso() });
    }

    return updateObjectByRow('TransferLogs', log.__row, {
      Status: 'Cancelled',
      ConfirmedBy: user.displayName || user.userName,
      ConfirmedAt: nowIso()
    });
  });
}
