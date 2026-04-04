'use strict';

/**
 * Admin page for MenuCraft.
 * Renders user management and group management UI for Admin users.
 *
 * @param {HTMLElement} container
 */
function renderAdminPage(container) {
  container.innerHTML = '';

  const page = document.createElement('div');
  page.className = 'admin-page';

  const title = document.createElement('h1');
  title.textContent = '管理画面';
  page.appendChild(title);

  // ── Tab buttons ──────────────────────────────────────────────────────────
  const tabBar = document.createElement('div');
  tabBar.className = 'admin-tabs';

  const userTabBtn = document.createElement('button');
  userTabBtn.className = 'admin-tab-btn active';
  userTabBtn.textContent = 'ユーザー';

  const groupTabBtn = document.createElement('button');
  groupTabBtn.className = 'admin-tab-btn';
  groupTabBtn.textContent = 'グループ';

  tabBar.append(userTabBtn, groupTabBtn);
  page.appendChild(tabBar);

  // ── Tab content ───────────────────────────────────────────────────────────
  const contentArea = document.createElement('div');
  contentArea.className = 'admin-tab-content';
  page.appendChild(contentArea);

  container.appendChild(page);

  // Load users tab by default
  _loadUsersTab(contentArea);

  userTabBtn.addEventListener('click', () => {
    userTabBtn.classList.add('active');
    groupTabBtn.classList.remove('active');
    _loadUsersTab(contentArea);
  });

  groupTabBtn.addEventListener('click', () => {
    groupTabBtn.classList.add('active');
    userTabBtn.classList.remove('active');
    _loadGroupsTab(contentArea);
  });
}

/**
 * Render the users tab.
 * @param {HTMLElement} area
 */
async function _loadUsersTab(area) {
  area.innerHTML = '';
  const loadingMsg = _createLoadingMsg();
  area.appendChild(loadingMsg);

  let users;
  try {
    users = await AdminApi.getUsers();
  } catch {
    _showTabError(area, 'ユーザー一覧の取得に失敗しました');
    return;
  }

  area.innerHTML = '';

  if (users.length === 0) {
    const empty = document.createElement('p');
    empty.textContent = 'ユーザーが存在しません';
    area.appendChild(empty);
    return;
  }

  const table = _createTable(['メール', 'ロール', 'グループ', '登録日', '操作']);
  const tbody = table.querySelector('tbody');

  for (const user of users) {
    const tr = document.createElement('tr');

    _appendCell(tr, user.email);
    _appendCell(tr, user.role);
    _appendCell(tr, user.groupName || '—');
    _appendCell(tr, _formatDate(user.createdAt));

    const actionTd = document.createElement('td');
    const roleBtn = document.createElement('button');
    roleBtn.className = 'btn-sm';
    roleBtn.textContent = user.role === 'Admin' ? 'Userに変更' : 'Adminに変更';
    roleBtn.addEventListener('click', async () => {
      const newRole = user.role === 'Admin' ? 'User' : 'Admin';
      if (!window.confirm(`${user.email} のロールを ${newRole} に変更しますか？`)) return;
      try {
        await AdminApi.changeRole(user.id, newRole);
        _loadUsersTab(area);
      } catch {
        alert('ロール変更に失敗しました');
      }
    });
    actionTd.appendChild(roleBtn);
    tr.appendChild(actionTd);

    tbody.appendChild(tr);
  }

  area.appendChild(table);
}

/**
 * Render the groups tab.
 * @param {HTMLElement} area
 */
async function _loadGroupsTab(area) {
  area.innerHTML = '';
  const loadingMsg = _createLoadingMsg();
  area.appendChild(loadingMsg);

  let groups;
  try {
    groups = await AdminApi.getGroups();
  } catch {
    _showTabError(area, 'グループ一覧の取得に失敗しました');
    return;
  }

  area.innerHTML = '';

  if (groups.length === 0) {
    const empty = document.createElement('p');
    empty.textContent = 'グループが存在しません';
    area.appendChild(empty);
    return;
  }

  const table = _createTable(['名前', '招待コード', 'メンバー数', '作成日', '操作']);
  const tbody = table.querySelector('tbody');

  for (const group of groups) {
    const tr = document.createElement('tr');

    _appendCell(tr, group.name);
    _appendCell(tr, group.inviteCode);
    _appendCell(tr, String(group.memberCount));
    _appendCell(tr, _formatDate(group.createdAt));

    const actionTd = document.createElement('td');

    const regenBtn = document.createElement('button');
    regenBtn.className = 'btn-sm';
    regenBtn.textContent = '招待コード再生成';
    regenBtn.addEventListener('click', async () => {
      if (!window.confirm('招待コードを再生成しますか？')) return;
      try {
        await AdminApi.regenerateInviteCode(group.id);
        _loadGroupsTab(area);
      } catch {
        alert('招待コード再生成に失敗しました');
      }
    });

    const deleteBtn = document.createElement('button');
    deleteBtn.className = 'btn-sm btn-danger';
    deleteBtn.textContent = '削除';
    deleteBtn.addEventListener('click', async () => {
      if (!window.confirm(`グループ「${group.name}」を削除しますか？この操作は取り消せません。`)) return;
      try {
        await AdminApi.deleteGroup(group.id);
        _loadGroupsTab(area);
      } catch {
        alert('グループ削除に失敗しました');
      }
    });

    actionTd.append(regenBtn, deleteBtn);
    tr.appendChild(actionTd);
    tbody.appendChild(tr);
  }

  area.appendChild(table);
}

// ── Private helpers ──────────────────────────────────────────────────────────

function _createTable(headers) {
  const table = document.createElement('table');
  table.className = 'admin-table';

  const thead = document.createElement('thead');
  const headerRow = document.createElement('tr');
  for (const h of headers) {
    const th = document.createElement('th');
    th.textContent = h;
    headerRow.appendChild(th);
  }
  thead.appendChild(headerRow);

  const tbody = document.createElement('tbody');
  table.append(thead, tbody);
  return table;
}

function _appendCell(row, text) {
  const td = document.createElement('td');
  td.textContent = text;
  row.appendChild(td);
}

function _formatDate(dateStr) {
  if (!dateStr) return '—';
  const d = new Date(dateStr);
  return isNaN(d.getTime()) ? '—' : d.toLocaleDateString('ja-JP');
}

function _createLoadingMsg() {
  const p = document.createElement('p');
  p.textContent = '読み込み中...';
  return p;
}

function _showTabError(area, msg) {
  area.innerHTML = '';
  const p = document.createElement('p');
  p.className = 'error-message';
  p.textContent = msg;
  area.appendChild(p);
}

// Export for testing (CommonJS); in browser renderAdminPage is a global
if (typeof module !== 'undefined' && module.exports) {
  module.exports = { renderAdminPage };
}
