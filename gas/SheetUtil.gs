/**
 * Generic helpers for treating Google Sheet tabs as simple tables.
 * Every table has a header row (row 1) and rows below are records.
 */

var SCHEMA = {
  Locations: ['Id', 'LocationName', 'Address', 'Latitude', 'Longitude', 'ContactPerson', 'Phone', 'IsActive', 'CreatedAt', 'UpdatedAt'],
  Items: ['Id', 'Category', 'ItemName', 'Specification', 'Quantity', 'Unit', 'StockType', 'ExpirationDate', 'ImagePath', 'LocationId', 'SafetyStock', 'Remark', 'IsActive', 'CreatedAt', 'UpdatedAt'],
  TransferLogs: ['Id', 'BatchId', 'SupplyItemId', 'FromLocationId', 'ToLocationId', 'TransferQuantity', 'TransferTime', 'Status', 'ConfirmedBy', 'ConfirmedAt', 'Operator', 'Remark'],
  OutboundLogs: ['Id', 'SupplyItemId', 'LocationId', 'OutboundQuantity', 'RecipientName', 'RecipientContact', 'Operator', 'OutboundTime', 'Remark'],
  Users: ['Id', 'UserName', 'PasswordHash', 'PasswordSalt', 'DisplayName', 'RoleName', 'LocationId', 'IsActive', 'CreatedAt', 'UpdatedAt'],
  Meta: ['TableName', 'NextId']
};

function nowIso() {
  return Utilities.formatDate(new Date(), 'Asia/Taipei', "yyyy-MM-dd'T'HH:mm:ss");
}

function getSs() {
  return SpreadsheetApp.getActiveSpreadsheet();
}

function ensureSheet(name) {
  var ss = getSs();
  var sheet = ss.getSheetByName(name);
  var headers = SCHEMA[name];
  if (!sheet) {
    sheet = ss.insertSheet(name);
  }
  if (sheet.getLastRow() === 0) {
    sheet.getRange(1, 1, 1, headers.length).setValues([headers]);
    sheet.setFrozenRows(1);
  }
  // Force plain-text formatting so Sheets never auto-converts our ISO date
  // strings (or anything else) into typed Date cells — that silently shifts
  // timestamps by the spreadsheet's own timezone offset on read-back.
  sheet.getRange(1, 1, 2000, headers.length).setNumberFormat('@');
  return sheet;
}

function getSheet(name) {
  var sheet = getSs().getSheetByName(name);
  if (!sheet) throw new Error('找不到資料表：' + name);
  return sheet;
}

function sheetToObjects(name) {
  var sheet = getSheet(name);
  var lastRow = sheet.getLastRow();
  var lastCol = sheet.getLastColumn();
  if (lastRow < 2) return [];
  var headers = sheet.getRange(1, 1, 1, lastCol).getValues()[0];
  var values = sheet.getRange(2, 1, lastRow - 1, lastCol).getValues();
  var out = [];
  for (var r = 0; r < values.length; r++) {
    var row = values[r];
    if (row[0] === '' || row[0] === null) continue; // skip blank rows
    var obj = {};
    for (var c = 0; c < headers.length; c++) {
      obj[headers[c]] = normalizeCell(row[c]);
    }
    obj.__row = r + 2; // 1-indexed sheet row, for fast update
    out.push(obj);
  }
  return out;
}

function normalizeCell(v) {
  if (v instanceof Date) return Utilities.formatDate(v, 'Asia/Taipei', "yyyy-MM-dd'T'HH:mm:ss");
  return v;
}

function appendObject(name, obj) {
  var sheet = getSheet(name);
  var headers = SCHEMA[name];
  var row = headers.map(function (h) {
    var v = obj[h];
    return (v === undefined || v === null) ? '' : v;
  });
  sheet.appendRow(row);
  return obj;
}

function findById(name, id) {
  var rows = sheetToObjects(name);
  for (var i = 0; i < rows.length; i++) {
    if (String(rows[i].Id) === String(id)) return rows[i];
  }
  return null;
}

function updateObjectByRow(name, rowIndex, patch) {
  var sheet = getSheet(name);
  var headers = SCHEMA[name];
  var current = sheet.getRange(rowIndex, 1, 1, headers.length).getValues()[0];
  var merged = {};
  for (var c = 0; c < headers.length; c++) merged[headers[c]] = current[c];
  for (var key in patch) {
    if (headers.indexOf(key) !== -1) merged[key] = patch[key];
  }
  var newRow = headers.map(function (h) {
    var v = merged[h];
    return (v === undefined || v === null) ? '' : v;
  });
  sheet.getRange(rowIndex, 1, 1, headers.length).setValues([newRow]);
  var out = {};
  for (var c2 = 0; c2 < headers.length; c2++) out[headers[c2]] = normalizeCell(newRow[c2]);
  return out;
}

function updateById(name, id, patch) {
  var existing = findById(name, id);
  if (!existing) throw new Error('找不到 Id=' + id + ' 的資料（' + name + '）');
  return updateObjectByRow(name, existing.__row, patch);
}

/** Atomic auto-increment id per table, using the Meta sheet + a script lock. */
function nextId(tableName) {
  return withLock(function () {
    var sheet = getSheet('Meta');
    var lastRow = sheet.getLastRow();
    var values = lastRow >= 2 ? sheet.getRange(2, 1, lastRow - 1, 2).getValues() : [];
    for (var i = 0; i < values.length; i++) {
      if (values[i][0] === tableName) {
        var next = Number(values[i][1]) || 1;
        sheet.getRange(i + 2, 2).setValue(next + 1);
        return next;
      }
    }
    sheet.appendRow([tableName, 2]);
    return 1;
  });
}

function withLock(fn) {
  var lock = LockService.getScriptLock();
  lock.waitLock(15000);
  try {
    return fn();
  } finally {
    lock.releaseLock();
  }
}

function toBool(v) {
  return v === true || v === 'TRUE' || v === 'true' || v === 1;
}
