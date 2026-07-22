function sanitizeUser(u) {
  return {
    Id: u.Id,
    UserName: u.UserName,
    DisplayName: u.DisplayName,
    RoleName: u.RoleName,
    LocationId: u.LocationId,
    IsActive: u.IsActive,
    CreatedAt: u.CreatedAt,
    UpdatedAt: u.UpdatedAt
  };
}

function Users_list(payload, token) {
  var user = requireAuth(token);
  requireRole(user, [Roles.Admin]);
  var rows = sheetToObjects('Users');
  rows.sort(function (a, b) { return Number(a.Id) - Number(b.Id); });
  return rows.map(sanitizeUser);
}

function findUserByName(userName, excludeId) {
  var rows = sheetToObjects('Users');
  for (var i = 0; i < rows.length; i++) {
    if (rows[i].UserName.toLowerCase() === userName.toLowerCase() && String(rows[i].Id) !== String(excludeId)) {
      return rows[i];
    }
  }
  return null;
}

function Users_create(payload, token) {
  var admin = requireAuth(token);
  requireRole(admin, [Roles.Admin]);

  var userName = (payload.userName || '').trim();
  if (!userName) throw new Error('請輸入帳號');
  if (!payload.password) throw new Error('請輸入密碼');
  if ([Roles.Admin, Roles.Cadre, Roles.SocialWorker].indexOf(payload.roleName) === -1) throw new Error('請選擇合法的角色');
  if (findUserByName(userName)) throw new Error('帳號已存在');

  var salt = makeSalt();
  var obj = {
    Id: nextId('Users'),
    UserName: userName,
    PasswordHash: hashPassword(payload.password, salt),
    PasswordSalt: salt,
    DisplayName: payload.displayName || userName,
    RoleName: payload.roleName,
    LocationId: payload.locationId || '',
    IsActive: true,
    CreatedAt: nowIso(),
    UpdatedAt: ''
  };
  appendObject('Users', obj);
  return sanitizeUser(obj);
}

function Users_update(payload, token) {
  var admin = requireAuth(token);
  requireRole(admin, [Roles.Admin]);
  if (!payload.id) throw new Error('缺少 id');

  var userName = (payload.userName || '').trim();
  if (!userName) throw new Error('請輸入帳號');
  if ([Roles.Admin, Roles.Cadre, Roles.SocialWorker].indexOf(payload.roleName) === -1) throw new Error('請選擇合法的角色');
  if (findUserByName(userName, payload.id)) throw new Error('帳號已存在');

  var patch = {
    UserName: userName,
    DisplayName: payload.displayName || userName,
    RoleName: payload.roleName,
    LocationId: payload.locationId || '',
    UpdatedAt: nowIso()
  };
  if (payload.password) {
    var salt = makeSalt();
    patch.PasswordSalt = salt;
    patch.PasswordHash = hashPassword(payload.password, salt);
  }
  var updated = updateById('Users', payload.id, patch);
  return sanitizeUser(updated);
}

function Users_delete(payload, token) {
  var admin = requireAuth(token);
  requireRole(admin, [Roles.Admin]);
  if (!payload.id) throw new Error('缺少 id');

  var target = findById('Users', payload.id);
  if (!target) throw new Error('找不到帳號');

  if (String(target.Id) === String(admin.uid)) throw new Error('無法停用目前登入中的帳號');

  if (target.RoleName === Roles.Admin) {
    var allUsers = sheetToObjects('Users');
    var otherActiveAdmins = allUsers.filter(function (u) {
      return u.RoleName === Roles.Admin && toBool(u.IsActive) && String(u.Id) !== String(target.Id);
    });
    if (otherActiveAdmins.length === 0) throw new Error('無法停用最後一位管理員帳號');
  }

  var updated = updateById('Users', payload.id, { IsActive: false, UpdatedAt: nowIso() });
  return sanitizeUser(updated);
}
