/**
 * One-time (idempotent) bootstrap: creates all sheet tabs with headers,
 * generates server secrets if missing, creates the image Drive folder,
 * and seeds the default admin account if the Users table is empty.
 *
 * Safe to call more than once — every step checks "does this already exist?" first.
 * Callable via the deployed Web App with action=setup, or by running
 * runSetup() directly from the Apps Script editor.
 */
function runSetup() {
  Object.keys(SCHEMA).forEach(function (name) { ensureSheet(name); });

  var props = PropertiesService.getScriptProperties();
  if (!props.getProperty('APP_SECRET')) {
    props.setProperty('APP_SECRET', Utilities.getUuid().replace(/-/g, ''));
  }
  if (!props.getProperty('TOKEN_SECRET')) {
    props.setProperty('TOKEN_SECRET', Utilities.getUuid().replace(/-/g, '') + Utilities.getUuid().replace(/-/g, ''));
  }
  if (!props.getProperty('IMAGE_FOLDER_ID')) {
    var folder = DriveApp.createFolder('ResourceSharingPlatform-Images');
    props.setProperty('IMAGE_FOLDER_ID', folder.getId());
  }

  var users = sheetToObjects('Users');
  if (users.length === 0) {
    var salt = makeSalt();
    appendObject('Users', {
      Id: nextId('Users'),
      UserName: 'admin',
      PasswordHash: hashPassword('admin', salt),
      PasswordSalt: salt,
      DisplayName: '系統管理員',
      RoleName: Roles.Admin,
      LocationId: '',
      IsActive: true,
      CreatedAt: nowIso(),
      UpdatedAt: ''
    });
  }

  return {
    message: '初始化完成',
    appSecret: props.getProperty('APP_SECRET'),
    sheets: Object.keys(SCHEMA)
  };
}

/** action: setup (no auth required — first-run bootstrap) */
function Setup_run(payload) {
  return runSetup();
}

/**
 * Danger: wipes every data sheet and rebuilds them from scratch (correct
 * text formatting), then reseeds the admin account. Only used once during
 * initial setup to fix bad test rows; requires an Admin token.
 */
function Setup_reset(payload, token) {
  var user = requireAuth(token);
  requireRole(user, [Roles.Admin]);

  Object.keys(SCHEMA).forEach(function (name) {
    var sheet = getSs().getSheetByName(name);
    if (sheet) getSs().deleteSheet(sheet);
  });
  Object.keys(SCHEMA).forEach(function (name) { ensureSheet(name); });

  var salt = makeSalt();
  appendObject('Users', {
    Id: nextId('Users'),
    UserName: 'admin',
    PasswordHash: hashPassword('admin', salt),
    PasswordSalt: salt,
    DisplayName: '系統管理員',
    RoleName: Roles.Admin,
    LocationId: '',
    IsActive: true,
    CreatedAt: nowIso(),
    UpdatedAt: ''
  });

  return { message: '已重置所有資料表' };
}
