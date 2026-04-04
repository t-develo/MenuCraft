'use strict';
/**
 * Tests for admin page (src/client/js/pages/admin.js)
 * @jest-environment jsdom
 */

let renderAdminPage;

beforeEach(() => {
  jest.resetModules();

  // Mock AdminApi global
  global.AdminApi = {
    getUsers: jest.fn().mockResolvedValue([]),
    changeRole: jest.fn().mockResolvedValue({}),
    getGroups: jest.fn().mockResolvedValue([]),
    updateGroup: jest.fn().mockResolvedValue({}),
    deleteGroup: jest.fn().mockResolvedValue(undefined),
    removeMember: jest.fn().mockResolvedValue(undefined),
    regenerateInviteCode: jest.fn().mockResolvedValue({ inviteCode: 'NEW123' }),
  };

  // Mock window.confirm
  window.confirm = jest.fn().mockReturnValue(true);

  const module = require('../../pages/admin');
  renderAdminPage = module.renderAdminPage || global.renderAdminPage;

  document.body.innerHTML = '<div id="container"></div>';
});

afterEach(() => {
  jest.restoreAllMocks();
  delete global.AdminApi;
});

describe('renderAdminPage', () => {
  test('コンテナに管理画面を描画する', async () => {
    const container = document.getElementById('container');

    renderAdminPage(container);

    expect(container.querySelector('.admin-page')).not.toBeNull();
  });

  test('ユーザータブとグループタブが存在する', () => {
    const container = document.getElementById('container');

    renderAdminPage(container);

    const tabs = container.querySelectorAll('.admin-tab-btn');
    const tabLabels = Array.from(tabs).map((t) => t.textContent);
    expect(tabLabels).toContain('ユーザー');
    expect(tabLabels).toContain('グループ');
  });

  test('初期表示でユーザーリストを読み込む', async () => {
    const container = document.getElementById('container');

    renderAdminPage(container);

    // Wait for async data load
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(global.AdminApi.getUsers).toHaveBeenCalled();
  });

  test('ユーザーデータをテーブルに描画する（XSS安全）', async () => {
    global.AdminApi.getUsers.mockResolvedValue([
      { id: 'u1', email: '<script>alert(1)</script>@test.com', role: 'Admin', groupName: null, createdAt: '2024-01-01T00:00:00Z' },
    ]);
    const container = document.getElementById('container');

    renderAdminPage(container);
    await new Promise((resolve) => setTimeout(resolve, 0));

    // Should not execute script — textContent used
    expect(container.innerHTML).not.toContain('<script>alert(1)</script>');
    expect(container.textContent).toContain('<script>alert(1)</script>@test.com');
  });

  test('グループタブをクリックするとグループリストを読み込む', async () => {
    const container = document.getElementById('container');
    renderAdminPage(container);

    const groupTab = Array.from(container.querySelectorAll('.admin-tab-btn'))
      .find((t) => t.textContent === 'グループ');
    groupTab.click();

    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(global.AdminApi.getGroups).toHaveBeenCalled();
  });

  test('グループデータをテーブルに描画する（XSS安全）', async () => {
    global.AdminApi.getGroups.mockResolvedValue([
      { id: 1, name: '<b>Family</b>', inviteCode: 'ABC', memberCount: 2, createdAt: '2024-01-01T00:00:00Z' },
    ]);
    const container = document.getElementById('container');
    renderAdminPage(container);

    const groupTab = Array.from(container.querySelectorAll('.admin-tab-btn'))
      .find((t) => t.textContent === 'グループ');
    groupTab.click();

    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(container.innerHTML).not.toContain('<b>Family</b>');
    expect(container.textContent).toContain('<b>Family</b>');
  });
});
