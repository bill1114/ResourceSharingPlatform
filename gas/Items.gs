var ALLOWED_IMAGE_MIME = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
var MAX_IMAGE_BASE64_CHARS = 7000000; // ~5MB source file, base64 is ~1.37x

function itemStatusOf(item, today) {
  if (Number(item.Quantity) <= Number(item.SafetyStock)) return 'LowStock';
  if (item.ExpirationDate) {
    var exp = new Date(item.ExpirationDate);
    if (exp < today) return 'Expired';
    var soon = new Date(today.getTime() + 30 * 24 * 60 * 60 * 1000);
    if (exp <= soon) return 'ExpiringSoon';
  }
  return 'Normal';
}

function uploadImageToDrive(base64, mimeType, fileName) {
  if (ALLOWED_IMAGE_MIME.indexOf(mimeType) === -1) throw new Error('圖片格式不支援（僅限 jpg/png/webp）');
  if (base64.length > MAX_IMAGE_BASE64_CHARS) throw new Error('圖片大小不可超過 5MB');

  var folderId = scriptProp('IMAGE_FOLDER_ID');
  var folder = DriveApp.getFolderById(folderId);
  var blob = Utilities.newBlob(Utilities.base64Decode(base64), mimeType, fileName || 'item.jpg');
  var file = folder.createFile(blob);
  file.setSharing(DriveApp.Access.ANYONE_WITH_LINK, DriveApp.Permission.VIEW);
  return 'https://drive.google.com/uc?export=view&id=' + file.getId();
}

function tryTrashDriveImage(imagePath) {
  if (!imagePath) return;
  var m = String(imagePath).match(/[?&]id=([^&]+)/);
  if (!m) return;
  try { DriveApp.getFileById(m[1]).setTrashed(true); } catch (e) { /* best-effort */ }
}

function Items_list(payload, token) {
  requireAuth(token);
  payload = payload || {};
  var rows = sheetToObjects('Items');
  if (!payload.includeInactive) rows = rows.filter(function (r) { return toBool(r.IsActive); });
  if (payload.locationId) rows = rows.filter(function (r) { return String(r.LocationId) === String(payload.locationId); });
  if (payload.category) rows = rows.filter(function (r) { return r.Category === payload.category; });
  if (payload.stockType) rows = rows.filter(function (r) { return r.StockType === payload.stockType; });
  rows.sort(function (a, b) { return Number(a.Id) - Number(b.Id); });
  rows.forEach(function (r) { delete r.__row; });
  return rows;
}

function Items_get(payload, token) {
  requireAuth(token);
  var item = findById('Items', payload.id);
  if (!item) throw new Error('找不到物資');
  delete item.__row;
  return item;
}

function validateItemPayload(payload) {
  if (!payload.category || !String(payload.category).trim()) throw new Error('請選擇分類');
  if (!payload.itemName || !String(payload.itemName).trim()) throw new Error('請輸入物資名稱');
  if (!payload.locationId) throw new Error('請選擇所屬據點');
  if (payload.quantity === undefined || Number(payload.quantity) < 0) throw new Error('數量不可為負');
  if (payload.safetyStock === undefined || Number(payload.safetyStock) < 0) throw new Error('安全庫存不可為負');
  var loc = findById('Locations', payload.locationId);
  if (!loc || !toBool(loc.IsActive)) throw new Error('所屬據點不存在或已停用');
}

function Items_create(payload, token) {
  var user = requireAuth(token);
  requireRole(user, [Roles.Admin, Roles.Cadre]);
  validateItemPayload(payload);

  var imagePath = '';
  if (payload.imageBase64) {
    imagePath = uploadImageToDrive(payload.imageBase64, payload.imageMimeType, payload.imageFileName);
  }

  var obj = {
    Id: nextId('Items'),
    Category: payload.category,
    ItemName: payload.itemName,
    Specification: payload.specification || '',
    Quantity: Number(payload.quantity),
    Unit: payload.unit || '',
    StockType: payload.stockType || 'NoExpiry',
    ExpirationDate: payload.expirationDate || '',
    ImagePath: imagePath,
    LocationId: Number(payload.locationId),
    SafetyStock: Number(payload.safetyStock),
    Remark: payload.remark || '',
    IsActive: true,
    CreatedAt: nowIso(),
    UpdatedAt: ''
  };
  appendObject('Items', obj);
  return obj;
}

function Items_update(payload, token) {
  var user = requireAuth(token);
  requireRole(user, [Roles.Admin, Roles.Cadre]);
  if (!payload.id) throw new Error('缺少 id');
  validateItemPayload(payload);

  var existing = findById('Items', payload.id);
  if (!existing) throw new Error('找不到物資');

  var imagePath = existing.ImagePath;
  if (payload.imageBase64) {
    imagePath = uploadImageToDrive(payload.imageBase64, payload.imageMimeType, payload.imageFileName);
    tryTrashDriveImage(existing.ImagePath);
  } else if (payload.removeImage) {
    tryTrashDriveImage(existing.ImagePath);
    imagePath = '';
  }

  return updateById('Items', payload.id, {
    Category: payload.category,
    ItemName: payload.itemName,
    Specification: payload.specification || '',
    Quantity: Number(payload.quantity),
    Unit: payload.unit || '',
    StockType: payload.stockType || 'NoExpiry',
    ExpirationDate: payload.expirationDate || '',
    ImagePath: imagePath,
    LocationId: Number(payload.locationId),
    SafetyStock: Number(payload.safetyStock),
    Remark: payload.remark || '',
    UpdatedAt: nowIso()
  });
}

function Items_delete(payload, token) {
  var user = requireAuth(token);
  requireRole(user, [Roles.Admin, Roles.Cadre]);
  if (!payload.id) throw new Error('缺少 id');
  return updateById('Items', payload.id, { IsActive: false, UpdatedAt: nowIso() });
}
