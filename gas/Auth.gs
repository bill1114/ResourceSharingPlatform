/**
 * Auth: password hashing (salted SHA-256) + signed, stateless session tokens.
 * A token is: base64url(JSON payload) + "." + base64url(HMAC-SHA256 signature)
 * The signing key (TOKEN_SECRET) never leaves the server.
 */

var Roles = { Admin: 'Admin', Cadre: 'Cadre', SocialWorker: 'SocialWorker' };
var TOKEN_TTL_MS = 8 * 60 * 60 * 1000; // 8 hours, matches the original cookie expiry

function scriptProp(key) {
  return PropertiesService.getScriptProperties().getProperty(key);
}

function makeSalt() {
  return Utilities.getUuid();
}

function hashPassword(password, salt) {
  var digest = Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256, password + ':' + salt);
  return digest.map(function (b) { return ('0' + (b & 0xFF).toString(16)).slice(-2); }).join('');
}

function base64urlEncode(str) {
  return Utilities.base64EncodeWebSafe(str, Utilities.Charset.UTF_8).replace(/=+$/, '');
}

function base64urlDecode(str) {
  return Utilities.newBlob(Utilities.base64DecodeWebSafe(str)).getDataAsString();
}

function signToken(claims) {
  var secret = scriptProp('TOKEN_SECRET');
  var payload = base64urlEncode(JSON.stringify(claims));
  var sigBytes = Utilities.computeHmacSha256Signature(payload, secret);
  var sig = Utilities.base64EncodeWebSafe(sigBytes).replace(/=+$/, '');
  return payload + '.' + sig;
}

function verifyToken(token) {
  if (!token || token.indexOf('.') === -1) throw new Error('未登入或登入已過期');
  var parts = token.split('.');
  var payload = parts[0], sig = parts[1];
  var secret = scriptProp('TOKEN_SECRET');
  var expectedSigBytes = Utilities.computeHmacSha256Signature(payload, secret);
  var expectedSig = Utilities.base64EncodeWebSafe(expectedSigBytes).replace(/=+$/, '');
  if (sig !== expectedSig) throw new Error('未登入或登入已過期');
  var claims = JSON.parse(base64urlDecode(payload));
  if (!claims.exp || claims.exp < Date.now()) throw new Error('登入已過期，請重新登入');
  return claims;
}

function requireAuth(token) {
  return verifyToken(token);
}

function requireRole(user, allowedRoles) {
  if (allowedRoles.indexOf(user.role) === -1) {
    throw new Error('權限不足');
  }
}

/** action: login */
function Auth_login(payload) {
  var userName = (payload.userName || '').trim();
  var password = payload.password || '';
  if (!userName || !password) throw new Error('請輸入帳號與密碼');

  var users = sheetToObjects('Users');
  var user = null;
  for (var i = 0; i < users.length; i++) {
    if (users[i].UserName && users[i].UserName.toLowerCase() === userName.toLowerCase()) {
      user = users[i];
      break;
    }
  }
  if (!user || !toBool(user.IsActive)) throw new Error('帳號或密碼錯誤');

  var hash = hashPassword(password, user.PasswordSalt);
  if (hash !== user.PasswordHash) throw new Error('帳號或密碼錯誤');

  var claims = {
    uid: user.Id,
    userName: user.UserName,
    displayName: user.DisplayName || user.UserName,
    role: user.RoleName,
    locationId: user.LocationId || null,
    exp: Date.now() + TOKEN_TTL_MS
  };
  var token = signToken(claims);
  return {
    token: token,
    Id: user.Id,
    UserName: user.UserName,
    DisplayName: claims.displayName,
    RoleName: user.RoleName,
    LocationId: user.LocationId || null
  };
}
