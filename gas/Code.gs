/**
 * Entry point + action router for the ResourceSharingPlatform Web App API.
 *
 * The static frontend (GitHub Pages) POSTs here with:
 *   { action: "...", secret: "...", token: "...", payload: {...} }
 * using a plain-string body (Content-Type: text/plain) so the browser never
 * sends a CORS preflight — Apps Script Web Apps cannot answer OPTIONS requests.
 *
 * "secret" is a shared app-level key (anti-abuse only, not real auth — it is
 * necessarily visible in the frontend source). Per-user identity/role comes
 * from the signed "token" issued at login (see Auth.gs) and is re-verified
 * on every call by each action handler via requireAuth()/requireRole().
 */

var ROUTES = {
  setup: Setup_run,
  resetData: Setup_reset,
  login: Auth_login,

  listLocations: Locations_list,
  getLocation: Locations_get,
  createLocation: Locations_create,
  updateLocation: Locations_update,
  deleteLocation: Locations_delete,

  listItems: Items_list,
  getItem: Items_get,
  createItem: Items_create,
  updateItem: Items_update,
  deleteItem: Items_delete,

  getDashboard: Dashboard_get,
  getMapLocations: MapApi_getLocations,

  listOutbound: Outbound_list,
  createOutbound: Outbound_create,

  listTransfers: Transfer_list,
  createTransferBatch: Transfer_createBatch,
  confirmTransfer: Transfer_confirm,
  cancelTransfer: Transfer_cancel,

  listUsers: Users_list,
  createUser: Users_create,
  updateUser: Users_update,
  deleteUser: Users_delete
};

// Actions that may run before the Sheets/secrets even exist yet.
var NO_SECRET_REQUIRED = { setup: true };

function doGet(e) {
  return handleRequest(e);
}

function doPost(e) {
  return handleRequest(e);
}

function handleRequest(e) {
  try {
    var body = parseRequestBody(e);
    var action = body.action;
    if (!action) return jsonOut({ success: false, message: '缺少 action' });

    var fn = ROUTES[action];
    if (!fn) return jsonOut({ success: false, message: '未知的操作：' + action });

    if (!NO_SECRET_REQUIRED[action]) {
      var expected = PropertiesService.getScriptProperties().getProperty('APP_SECRET');
      if (!expected || body.secret !== expected) {
        return jsonOut({ success: false, message: '密鑰錯誤' });
      }
    }

    var result = fn(body.payload || {}, body.token);
    return jsonOut({ success: true, data: result });
  } catch (err) {
    return jsonOut({ success: false, message: err && err.message ? err.message : String(err) });
  }
}

function parseRequestBody(e) {
  if (e && e.postData && e.postData.contents) {
    return JSON.parse(e.postData.contents);
  }
  if (e && e.parameter && e.parameter.action) {
    var body = { action: e.parameter.action, secret: e.parameter.secret, token: e.parameter.token };
    body.payload = e.parameter.payload ? JSON.parse(e.parameter.payload) : {};
    return body;
  }
  return {};
}

function jsonOut(obj) {
  return ContentService.createTextOutput(JSON.stringify(obj)).setMimeType(ContentService.MimeType.JSON);
}
