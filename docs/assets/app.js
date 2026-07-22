// Shared helpers for the static (GitHub Pages) frontend.
// Talks directly to the Google Apps Script Web App configured below.
//
// KNOWN LIMITATION: GAS_URL / GAS_SECRET are visible to anyone viewing this
// file, so the secret does not protect the data from a determined visitor -
// it only stops casual/automated abuse. Role checks below are cosmetic
// (hide/show UI); they are not enforced server-side.

const GAS_URL = 'https://script.google.com/macros/s/AKfycbzqm3b_9d8OXejNB56-IPYSzW65Sab3Eg-GE2XjEOrNIMIhYuV_uWWIeYLquISCFi0b0A/exec';
const GAS_SECRET = 'GV3ceMIBUmB24anzPAXUf4aHbx9VE4Tp';

const Roles = {
  Admin: 'Admin',
  Cadre: 'Cadre',
  SocialWorker: 'SocialWorker',
  toDisplayName(role) {
    switch (role) {
      case 'Admin': return '管理人員';
      case 'Cadre': return '幹部';
      case 'SocialWorker': return '社工';
      default: return role || '';
    }
  }
};

const StockTypes = {
  NoExpiry: 'NoExpiry',
  HasExpiry: 'HasExpiry',
  Frozen: 'Frozen',
  all: ['NoExpiry', 'HasExpiry', 'Frozen'],
  toDisplayName(type) {
    switch (type) {
      case 'NoExpiry': return '無效期物資';
      case 'HasExpiry': return '有效期物資';
      case 'Frozen': return '冷凍食品';
      default: return type || '';
    }
  },
  toBadgeClass(type) {
    switch (type) {
      case 'NoExpiry': return 'bg-primary';
      case 'HasExpiry': return 'bg-success';
      case 'Frozen': return 'bg-info text-dark';
      default: return 'bg-secondary';
    }
  }
};

const TransferStatuses = {
  Pending: 'Pending',
  Confirmed: 'Confirmed',
  Cancelled: 'Cancelled',
  toDisplayName(status) {
    switch (status) {
      case 'Pending': return '待確認';
      case 'Confirmed': return '已確認';
      case 'Cancelled': return '已取消';
      default: return status || '';
    }
  },
  toBadgeClass(status) {
    switch (status) {
      case 'Pending': return 'bg-warning text-dark';
      case 'Confirmed': return 'bg-success';
      case 'Cancelled': return 'bg-secondary';
      default: return 'bg-light text-dark';
    }
  }
};

function itemStatus(item) {
  const today = new Date(); today.setHours(0, 0, 0, 0);
  if (item.Quantity <= item.SafetyStock) return { text: '低庫存', cls: 'bg-danger' };
  if (item.ExpirationDate) {
    const exp = new Date(item.ExpirationDate);
    if (exp < today) return { text: '已過期', cls: 'bg-dark' };
    const soon = new Date(today); soon.setDate(soon.getDate() + 30);
    if (exp <= soon) return { text: '即將過期', cls: 'bg-warning text-dark' };
  }
  return { text: '正常', cls: 'bg-success' };
}

const LOCATION_PALETTE = [
  ['#0d6efd', '#fff'], ['#6f42c1', '#fff'], ['#d63384', '#fff'], ['#fd7e14', '#000'],
  ['#20c997', '#000'], ['#6610f2', '#fff'], ['#198754', '#fff'], ['#0dcaf0', '#000']
];

function locationBadgeStyle(locationId) {
  const idx = ((locationId - 1) % LOCATION_PALETTE.length + LOCATION_PALETTE.length) % LOCATION_PALETTE.length;
  const [bg, text] = LOCATION_PALETTE[idx];
  return `background-color:${bg};color:${text};`;
}

async function callApi(action, payload) {
  const res = await fetch(GAS_URL, {
    method: 'POST',
    body: JSON.stringify({ action, secret: GAS_SECRET, payload: payload || {} })
  });
  if (!res.ok) {
    throw new Error('網路錯誤：' + res.status);
  }
  return res.json();
}

async function sha256Hex(text) {
  const data = new TextEncoder().encode(text);
  const digest = await crypto.subtle.digest('SHA-256', data);
  return Array.from(new Uint8Array(digest)).map(b => b.toString(16).padStart(2, '0')).join('');
}

function currentUser() {
  const raw = sessionStorage.getItem('rsp_user');
  return raw ? JSON.parse(raw) : null;
}

function requireLogin() {
  const user = currentUser();
  if (!user) {
    location.href = 'index.html';
    return null;
  }
  return user;
}

function logout() {
  sessionStorage.removeItem('rsp_user');
  location.href = 'index.html';
}

function fmtDateTime(iso) {
  if (!iso) return '';
  return iso.replace('T', ' ').slice(0, 16);
}

function fmtDate(iso) {
  if (!iso) return '';
  return iso.slice(0, 10);
}

function escapeHtml(s) {
  return String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

function showAlert(containerId, type, message) {
  const el = document.getElementById(containerId);
  if (!el) return;
  el.innerHTML = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
    ${escapeHtml(message)}
    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
  </div>`;
}

function renderNav(activePage) {
  const user = currentUser();
  const placeholder = document.getElementById('nav-placeholder');
  if (!placeholder || !user) return;

  const isAdmin = user.RoleName === Roles.Admin;
  const isCadre = user.RoleName === Roles.Cadre;
  const link = (page, href, icon, label) => `
    <li class="nav-item">
      <a class="nav-link text-white${activePage === page ? ' active fw-bold' : ''}" href="${href}">
        <i class="bi ${icon}"></i> ${label}
      </a>
    </li>`;

  let links = '';
  links += link('dashboard', 'dashboard.html', 'bi-speedometer2', '戰情總覽');
  links += link('map', 'map.html', 'bi-geo-alt', '據點地圖');
  links += link('items', 'items.html', 'bi-box', '物資管理');
  links += link('locations', 'locations.html', 'bi-buildings', '據點管理');
  if (isAdmin || isCadre) {
    links += link('transfer', 'transfer.html', 'bi-arrow-left-right', '物資轉移');
  }
  links += link('outbound', 'outbound.html', 'bi-box-arrow-up', '物資出庫');
  if (isAdmin) {
    links += link('users', 'users.html', 'bi-people', '帳號管理');
  }

  placeholder.innerHTML = `
    <nav class="navbar navbar-expand-sm navbar-dark bg-primary border-bottom box-shadow mb-3">
      <div class="container-fluid">
        <a class="navbar-brand" href="dashboard.html"><i class="bi bi-box-seam"></i> 地方物資管理平台</a>
        <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navMain">
          <span class="navbar-toggler-icon"></span>
        </button>
        <div class="collapse navbar-collapse" id="navMain">
          <ul class="navbar-nav flex-grow-1">${links}</ul>
          <ul class="navbar-nav">
            <li class="nav-item d-flex align-items-center text-white me-3">
              <i class="bi bi-person-circle"></i>
              <span class="ms-1">${escapeHtml(user.DisplayName || user.UserName)}</span>
              <span class="badge bg-light text-dark ms-2">${Roles.toDisplayName(user.RoleName)}</span>
            </li>
            <li class="nav-item">
              <button type="button" class="btn btn-sm btn-outline-light" onclick="logout()">
                <i class="bi bi-box-arrow-right"></i> 登出
              </button>
            </li>
          </ul>
        </div>
      </div>
    </nav>`;
}
