/**
 * ResourceSharingPlatform - Google Sheets backend (Apps Script Web App)
 *
 * Standalone script that talks to a Google Sheet (by Id) with 5 tabs matching
 * the SCHEMAS below (header row = column names, in order). See SETUP.md.
 *
 * All requests are POSTed as JSON: { action, secret, payload }
 * The secret must match the "API_SECRET" Script Property.
 */

var SPREADSHEET_ID = 'REPLACE_WITH_YOUR_SPREADSHEET_ID';

var SCHEMAS = {
  SupplyLocation: [
    ['Id', 'int'], ['LocationName', 'string'], ['Address', 'string'],
    ['Latitude', 'float'], ['Longitude', 'float'], ['ContactPerson', 'string'],
    ['Phone', 'string'], ['IsActive', 'bool'], ['CreatedAt', 'datetime'], ['UpdatedAt', 'datetime']
  ],
  SupplyItem: [
    ['Id', 'int'], ['Category', 'string'], ['ItemName', 'string'], ['Specification', 'string'],
    ['Quantity', 'int'], ['Unit', 'string'], ['StockType', 'string'], ['ExpirationDate', 'date'],
    ['ImagePath', 'string'], ['LocationId', 'int'], ['SafetyStock', 'int'], ['Remark', 'string'],
    ['IsActive', 'bool'], ['CreatedAt', 'datetime'], ['UpdatedAt', 'datetime']
  ],
  SupplyTransferLog: [
    ['Id', 'int'], ['BatchId', 'string'], ['SupplyItemId', 'int'], ['FromLocationId', 'int'],
    ['ToLocationId', 'int'], ['TransferQuantity', 'int'], ['TransferTime', 'datetime'],
    ['Status', 'string'], ['ConfirmedBy', 'string'], ['ConfirmedAt', 'datetime'],
    ['Operator', 'string'], ['Remark', 'string']
  ],
  SupplyOutboundLog: [
    ['Id', 'int'], ['SupplyItemId', 'int'], ['LocationId', 'int'], ['OutboundQuantity', 'int'],
    ['RecipientName', 'string'], ['RecipientContact', 'string'], ['Operator', 'string'],
    ['OutboundTime', 'datetime'], ['Remark', 'string']
  ],
  UserAccount: [
    ['Id', 'int'], ['UserName', 'string'], ['PasswordHash', 'string'], ['DisplayName', 'string'],
    ['RoleName', 'string'], ['LocationId', 'int'], ['IsActive', 'bool'],
    ['CreatedAt', 'datetime'], ['UpdatedAt', 'datetime']
  ]
};

// ---------------------------------------------------------------------------
// Web app entry points
// ---------------------------------------------------------------------------

function doPost(e) {
  return handleRequest_(e);
}

function doGet(e) {
  return handleRequest_(e);
}

function handleRequest_(e) {
  var response;
  try {
    var body = parseRequestBody_(e);
    var expected = PropertiesService.getScriptProperties().getProperty('API_SECRET');

    if (!expected || body.secret !== expected) {
      response = { success: false, message: 'Unauthorized' };
    } else {
      var handler = ACTIONS[body.action];
      if (!handler) {
        response = { success: false, message: 'Unknown action: ' + body.action };
      } else {
        response = handler(body.payload || {});
      }
    }
  } catch (err) {
    response = { success: false, message: 'Server error: ' + err.message };
  }
  return jsonOutput_(response);
}

function parseRequestBody_(e) {
  if (e && e.postData && e.postData.contents) {
    return JSON.parse(e.postData.contents);
  }
  if (e && e.parameter && e.parameter.body) {
    return JSON.parse(e.parameter.body);
  }
  return {};
}

function jsonOutput_(obj) {
  return ContentService.createTextOutput(JSON.stringify(obj)).setMimeType(ContentService.MimeType.JSON);
}

// ---------------------------------------------------------------------------
// One-time setup helper - run manually from the Apps Script editor once
// (select "setupSheets" in the function dropdown, then Run).
// ---------------------------------------------------------------------------

function setupSheets() {
  var ss = SpreadsheetApp.openById(SPREADSHEET_ID);
  Object.keys(SCHEMAS).forEach(function (name) {
    var sheet = ss.getSheetByName(name);
    if (!sheet) {
      sheet = ss.insertSheet(name);
    }
    if (sheet.getLastRow() === 0) {
      var headers = SCHEMAS[name].map(function (col) { return col[0]; });
      sheet.getRange(1, 1, 1, headers.length).setValues([headers]);
      sheet.setFrozenRows(1);
    }
  });
  ['Sheet1', '工作表1'].forEach(function (name) {
    var defaultSheet = ss.getSheetByName(name);
    if (defaultSheet && defaultSheet.getLastRow() === 0 && defaultSheet.getLastColumn() <= 1) {
      ss.deleteSheet(defaultSheet);
    }
  });
}

// ---------------------------------------------------------------------------
// Generic sheet helpers
// ---------------------------------------------------------------------------

function sheet_(name) {
  var sheet = SpreadsheetApp.openById(SPREADSHEET_ID).getSheetByName(name);
  if (!sheet) {
    throw new Error('Sheet not found: ' + name);
  }
  return sheet;
}

function coerceIn_(type, value) {
  if (value === null || value === undefined || value === '') {
    return type === 'string' ? '' : null;
  }
  switch (type) {
    case 'int': return Math.trunc(Number(value));
    case 'float': return Number(value);
    case 'bool': return value === true || value === 'true' || value === 'TRUE' || value === 1;
    case 'date': return toIsoDate_(new Date(value));
    case 'datetime': return toIsoDateTime_(new Date(value));
    default: return String(value);
  }
}

function coerceOut_(type, value) {
  if (value === '' || value === null || value === undefined) {
    return type === 'string' ? (value === undefined ? '' : value) : null;
  }
  switch (type) {
    case 'int': return Math.trunc(Number(value));
    case 'float': return Number(value);
    case 'bool': return value === true || value === 'TRUE';
    case 'date':
      return (value instanceof Date) ? toIsoDate_(value) : String(value);
    case 'datetime':
      return (value instanceof Date) ? toIsoDateTime_(value) : String(value);
    default: return value;
  }
}

function toIsoDate_(date) {
  return Utilities.formatDate(date, Session.getScriptTimeZone(), 'yyyy-MM-dd');
}

function toIsoDateTime_(date) {
  return Utilities.formatDate(date, Session.getScriptTimeZone(), "yyyy-MM-dd'T'HH:mm:ss");
}

// Reads the whole sheet into an array of plain objects using SCHEMAS for typing.
function readAll_(name) {
  var sheet = sheet_(name);
  var schema = SCHEMAS[name];
  var lastRow = sheet.getLastRow();
  if (lastRow < 2) return [];

  var values = sheet.getRange(2, 1, lastRow - 1, schema.length).getValues();
  return values
    .filter(function (row) { return row[0] !== '' && row[0] !== null; })
    .map(function (row) { return rowToObject_(schema, row); });
}

function rowToObject_(schema, row) {
  var obj = {};
  for (var i = 0; i < schema.length; i++) {
    obj[schema[i][0]] = coerceOut_(schema[i][1], row[i]);
  }
  return obj;
}

function objectToRow_(schema, obj) {
  return schema.map(function (col) { return coerceIn_(col[1], obj[col[0]]); });
}

function findRowIndexById_(sheet, schema, id) {
  var lastRow = sheet.getLastRow();
  if (lastRow < 2) return -1;
  var idColValues = sheet.getRange(2, 1, lastRow - 1, 1).getValues();
  for (var i = 0; i < idColValues.length; i++) {
    if (Number(idColValues[i][0]) === Number(id)) {
      return i + 2; // 1-based sheet row
    }
  }
  return -1;
}

function nextId_(name) {
  var rows = readAll_(name);
  var maxId = rows.reduce(function (max, r) { return Math.max(max, Number(r.Id) || 0); }, 0);
  return maxId + 1;
}

function insertEntity_(name, obj) {
  var sheet = sheet_(name);
  var schema = SCHEMAS[name];
  obj.Id = nextId_(name);
  var row = objectToRow_(schema, obj);
  sheet.appendRow(row);
  return rowToObject_(schema, row);
}

function updateEntity_(name, obj) {
  var sheet = sheet_(name);
  var schema = SCHEMAS[name];
  var rowIndex = findRowIndexById_(sheet, schema, obj.Id);
  if (rowIndex === -1) {
    throw new Error(name + ' Id=' + obj.Id + ' not found');
  }
  var row = objectToRow_(schema, obj);
  sheet.getRange(rowIndex, 1, 1, row.length).setValues([row]);
  return rowToObject_(schema, row);
}

function findById_(name, id) {
  var rows = readAll_(name);
  for (var i = 0; i < rows.length; i++) {
    if (Number(rows[i].Id) === Number(id)) return rows[i];
  }
  return null;
}

function withLock_(fn) {
  var lock = LockService.getScriptLock();
  lock.waitLock(10000);
  try {
    return fn();
  } finally {
    lock.releaseLock();
  }
}

// ---------------------------------------------------------------------------
// Actions
// ---------------------------------------------------------------------------

var ACTIONS = {
  ping: function () {
    return { success: true, message: 'pong' };
  },
  // Callable over HTTP so the one-time sheet setup can be triggered without
  // using the Apps Script editor's function picker.
  setupSheetsHttp: function () {
    setupSheets();
    return { success: true, message: 'sheets ready' };
  },

  // ---- reads ----
  getLocations: function () {
    return { success: true, data: readAll_('SupplyLocation') };
  },
  getItems: function () {
    return { success: true, data: readAll_('SupplyItem') };
  },
  getUsers: function () {
    return { success: true, data: readAll_('UserAccount') };
  },
  getUserByUsername: function (p) {
    var rows = readAll_('UserAccount');
    var found = rows.find(function (u) { return u.UserName === p.userName; }) || null;
    return { success: true, data: found };
  },
  getTransferLogs: function () {
    return { success: true, data: readAll_('SupplyTransferLog') };
  },
  getOutboundLogs: function () {
    return { success: true, data: readAll_('SupplyOutboundLog') };
  },

  // ---- simple CRUD ----
  createLocation: function (p) {
    return withLock_(function () {
      var now = new Date();
      p.location.IsActive = true;
      p.location.CreatedAt = p.location.CreatedAt || now;
      return { success: true, data: insertEntity_('SupplyLocation', p.location) };
    });
  },
  updateLocation: function (p) {
    return withLock_(function () {
      return { success: true, data: updateEntity_('SupplyLocation', p.location) };
    });
  },
  createItem: function (p) {
    return withLock_(function () {
      var now = new Date();
      p.item.IsActive = true;
      p.item.CreatedAt = p.item.CreatedAt || now;
      return { success: true, data: insertEntity_('SupplyItem', p.item) };
    });
  },
  updateItem: function (p) {
    return withLock_(function () {
      return { success: true, data: updateEntity_('SupplyItem', p.item) };
    });
  },
  createUser: function (p) {
    return withLock_(function () {
      var now = new Date();
      p.user.CreatedAt = p.user.CreatedAt || now;
      return { success: true, data: insertEntity_('UserAccount', p.user) };
    });
  },
  updateUser: function (p) {
    return withLock_(function () {
      return { success: true, data: updateEntity_('UserAccount', p.user) };
    });
  },

  // ---- atomic business operations ----
  createTransferBatch: function (p) {
    return withLock_(function () {
      var now = new Date();
      var lines = p.lines || [];
      var sourceItems = [];

      // Validate every line first - all or nothing, matching the original
      // EF Core transaction semantics (no partial writes on failure).
      for (var i = 0; i < lines.length; i++) {
        var line = lines[i];
        var item = findById_('SupplyItem', line.supplyItemId);
        if (!item || item.LocationId !== p.fromLocationId || !item.IsActive) {
          return { success: false, message: '找不到來源物資（Id=' + line.supplyItemId + '）' };
        }
        if (item.Quantity < line.transferQuantity) {
          return { success: false, message: '「' + item.ItemName + '」來源數量不足，目前只有 ' + item.Quantity + ' ' + (item.Unit || '') };
        }
        sourceItems.push(item);
      }

      var batchId = Utilities.getUuid();
      for (var j = 0; j < lines.length; j++) {
        var srcItem = sourceItems[j];
        srcItem.Quantity -= lines[j].transferQuantity;
        srcItem.UpdatedAt = now;
        updateEntity_('SupplyItem', srcItem);

        insertEntity_('SupplyTransferLog', {
          BatchId: batchId,
          SupplyItemId: srcItem.Id,
          FromLocationId: p.fromLocationId,
          ToLocationId: p.toLocationId,
          TransferQuantity: lines[j].transferQuantity,
          TransferTime: now,
          Status: 'Pending',
          Operator: p.operatorName,
          Remark: p.remark
        });
      }

      return { success: true, message: '轉移已建立，共 ' + lines.length + ' 項物資，待對方確認送達後才會計入目標據點庫存' };
    });
  },

  confirmTransfer: function (p) {
    return withLock_(function () {
      var log = findById_('SupplyTransferLog', p.logId);
      if (!log || log.Status !== 'Pending') {
        return { success: false, message: '找不到待確認的轉移紀錄，可能已經處理過' };
      }

      var now = new Date();
      var sourceItem = findById_('SupplyItem', log.SupplyItemId);
      if (!sourceItem) {
        return { success: false, message: '找不到對應的物資資料' };
      }

      var items = readAll_('SupplyItem');
      var targetItem = items.find(function (x) {
        return x.ItemName === sourceItem.ItemName &&
          x.Category === sourceItem.Category &&
          x.LocationId === log.ToLocationId &&
          x.ExpirationDate === sourceItem.ExpirationDate &&
          x.IsActive;
      });

      if (!targetItem) {
        insertEntity_('SupplyItem', {
          Category: sourceItem.Category,
          ItemName: sourceItem.ItemName,
          Specification: sourceItem.Specification,
          Quantity: log.TransferQuantity,
          Unit: sourceItem.Unit,
          StockType: sourceItem.StockType,
          ExpirationDate: sourceItem.ExpirationDate,
          LocationId: log.ToLocationId,
          SafetyStock: sourceItem.SafetyStock,
          Remark: sourceItem.Remark,
          IsActive: true,
          CreatedAt: now
        });
      } else {
        targetItem.Quantity += log.TransferQuantity;
        targetItem.UpdatedAt = now;
        updateEntity_('SupplyItem', targetItem);
      }

      log.Status = 'Confirmed';
      log.ConfirmedBy = p.confirmedBy;
      log.ConfirmedAt = now;
      updateEntity_('SupplyTransferLog', log);

      return { success: true, message: '「' + sourceItem.ItemName + '」已確認送達，目標據點庫存已更新' };
    });
  },

  cancelTransfer: function (p) {
    return withLock_(function () {
      var log = findById_('SupplyTransferLog', p.logId);
      if (!log || log.Status !== 'Pending') {
        return { success: false, message: '找不到待確認的轉移紀錄，可能已經處理過' };
      }

      var now = new Date();
      var sourceItem = findById_('SupplyItem', log.SupplyItemId);
      if (sourceItem) {
        sourceItem.Quantity += log.TransferQuantity;
        sourceItem.UpdatedAt = now;
        updateEntity_('SupplyItem', sourceItem);
      }

      log.Status = 'Cancelled';
      log.ConfirmedBy = p.cancelledBy;
      log.ConfirmedAt = now;
      updateEntity_('SupplyTransferLog', log);

      return { success: true, message: '「' + (sourceItem ? sourceItem.ItemName : '') + '」轉移已取消，來源據點庫存已退回' };
    });
  },

  issueOutbound: function (p) {
    return withLock_(function () {
      var item = findById_('SupplyItem', p.supplyItemId);
      if (!item || !item.IsActive) {
        return { success: false, message: '找不到指定的物資' };
      }
      if (item.Quantity < p.outboundQuantity) {
        return { success: false, message: '庫存數量不足，目前僅有 ' + item.Quantity + ' ' + (item.Unit || '') };
      }

      var now = new Date();
      item.Quantity -= p.outboundQuantity;
      item.UpdatedAt = now;
      updateEntity_('SupplyItem', item);

      insertEntity_('SupplyOutboundLog', {
        SupplyItemId: item.Id,
        LocationId: item.LocationId,
        OutboundQuantity: p.outboundQuantity,
        RecipientName: p.recipientName,
        RecipientContact: p.recipientContact,
        Operator: p.operatorName,
        OutboundTime: now,
        Remark: p.remark
      });

      return { success: true, message: '出庫完成' };
    });
  }
};
