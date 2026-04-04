'use strict';
/**
 * Tests for app.js - role-based navigation and routing
 * @jest-environment jsdom
 */

// Helper: create a JWT with given payload (no real signature needed for frontend tests)
function makeJwt(payload) {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.fake-signature`;
}

// Helpers: render pages are stubbed
function setupGlobals({ role = 'User', familyGroupId = 1 } = {}) {
  const token = makeJwt({
    sub: 'user-1',
    email: 'test@example.com',
    role,
    familyGroupId,
    exp: Math.floor(Date.now() / 1000) + 3600,
  });

  const storage = { authToken: token };
  jest.spyOn(Storage.prototype, 'getItem').mockImplementation((k) => storage[k] ?? null);
  jest.spyOn(Storage.prototype, 'removeItem').mockImplementation((k) => { delete storage[k]; });
  jest.spyOn(Storage.prototype, 'setItem').mockImplementation((k, v) => { storage[k] = v; });

  delete window.location;
  window.location = { href: '', hash: '' };

  // Stub page render functions
  global.renderMealPlanBoard = jest.fn();
  global.renderRecipesPage = jest.fn();
  global.renderShoppingListPage = jest.fn();
  global.renderGroupSetup = jest.fn();
  global.renderAdminPage = jest.fn();
  global.renderMyPage = jest.fn();

  document.body.innerHTML = `
    <div id="app">
      <header id="app-header"></header>
      <main id="app-main"></main>
    </div>
  `;
}

beforeEach(() => {
  jest.resetModules();
});

afterEach(() => {
  jest.restoreAllMocks();
  [
    'renderMealPlanBoard', 'renderRecipesPage', 'renderShoppingListPage',
    'renderGroupSetup', 'renderAdminPage', 'renderMyPage',
  ].forEach((fn) => delete global[fn]);
});

describe('app.js - ナビゲーション', () => {
  test('全ユーザーに「マイページ」ナビリンクが表示される (User ロール)', () => {
    setupGlobals({ role: 'User' });
    require('../../app');

    const nav = document.querySelector('.app-nav');
    const buttons = Array.from(nav.querySelectorAll('button.nav-link'));
    expect(buttons.some((b) => b.textContent === 'マイページ')).toBe(true);
  });

  test('全ユーザーに「マイページ」ナビリンクが表示される (Admin ロール)', () => {
    setupGlobals({ role: 'Admin' });
    require('../../app');

    const nav = document.querySelector('.app-nav');
    const buttons = Array.from(nav.querySelectorAll('button.nav-link'));
    expect(buttons.some((b) => b.textContent === 'マイページ')).toBe(true);
  });

  test('Admin ユーザーには「管理」ナビリンクが表示される', () => {
    setupGlobals({ role: 'Admin' });
    require('../../app');

    const nav = document.querySelector('.app-nav');
    const buttons = Array.from(nav.querySelectorAll('button.nav-link'));
    expect(buttons.some((b) => b.textContent === '管理')).toBe(true);
  });

  test('User ロールには「管理」ナビリンクが表示されない', () => {
    setupGlobals({ role: 'User' });
    require('../../app');

    const nav = document.querySelector('.app-nav');
    const buttons = Array.from(nav.querySelectorAll('button.nav-link'));
    expect(buttons.some((b) => b.textContent === '管理')).toBe(false);
  });
});

describe('app.js - ルーティング', () => {
  test('mypage ルートで renderMyPage が呼ばれる', () => {
    setupGlobals({ role: 'User' });
    window.location.hash = '#mypage';
    require('../../app');

    expect(global.renderMyPage).toHaveBeenCalled();
  });

  test('admin ルートで Admin ユーザーは renderAdminPage が呼ばれる', () => {
    setupGlobals({ role: 'Admin' });
    window.location.hash = '#admin';
    require('../../app');

    expect(global.renderAdminPage).toHaveBeenCalled();
  });

  test('admin ルートで User ユーザーは renderAdminPage が呼ばれない', () => {
    setupGlobals({ role: 'User' });
    window.location.hash = '#admin';
    require('../../app');

    // User accessing admin route should not render admin page
    expect(global.renderAdminPage).not.toHaveBeenCalled();
  });
});
