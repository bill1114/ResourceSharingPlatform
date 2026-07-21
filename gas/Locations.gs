function Locations_list(payload, token) {
  requireAuth(token);
  var rows = sheetToObjects('Locations');
  if (!payload.includeInactive) rows = rows.filter(function (r) { return toBool(r.IsActive); });
  rows.sort(function (a, b) { return Number(a.Id) - Number(b.Id); });
  rows.forEach(function (r) { delete r.__row; });
  return rows;
}

function Locations_get(payload, token) {
  requireAuth(token);
  var loc = findById('Locations', payload.id);
  if (!loc) throw new Error('找不到據點');
  delete loc.__row;
  return loc;
}

function Locations_create(payload, token) {
  var user = requireAuth(token);
  requireRole(user, [Roles.Admin, Roles.Cadre]);
  if (!payload.locationName || !String(payload.locationName).trim()) throw new Error('請輸入據點名稱');

  var obj = {
    Id: nextId('Locations'),
    LocationName: payload.locationName,
    Address: payload.address || '',
    Latitude: payload.latitude || '',
    Longitude: payload.longitude || '',
    ContactPerson: payload.contactPerson || '',
    Phone: payload.phone || '',
    IsActive: true,
    CreatedAt: nowIso(),
    UpdatedAt: ''
  };
  appendObject('Locations', obj);
  return obj;
}

function Locations_update(payload, token) {
  var user = requireAuth(token);
  requireRole(user, [Roles.Admin, Roles.Cadre]);
  if (!payload.id) throw new Error('缺少 id');
  if (!payload.locationName || !String(payload.locationName).trim()) throw new Error('請輸入據點名稱');

  return updateById('Locations', payload.id, {
    LocationName: payload.locationName,
    Address: payload.address || '',
    Latitude: payload.latitude || '',
    Longitude: payload.longitude || '',
    ContactPerson: payload.contactPerson || '',
    Phone: payload.phone || '',
    UpdatedAt: nowIso()
  });
}

function Locations_delete(payload, token) {
  var user = requireAuth(token);
  requireRole(user, [Roles.Admin, Roles.Cadre]);
  if (!payload.id) throw new Error('缺少 id');
  return updateById('Locations', payload.id, { IsActive: false, UpdatedAt: nowIso() });
}
