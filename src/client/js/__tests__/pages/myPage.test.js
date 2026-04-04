'use strict';
/**
 * Tests for myPage (src/client/js/pages/myPage.js)
 * @jest-environment jsdom
 */

let renderMyPage;

beforeEach(() => {
  jest.resetModules();

  global.ProfileApi = {
    getProfile: jest.fn().mockResolvedValue({
      id: 'user-1',
      email: 'test@example.com',
      role: 'User',
      familyGroupId: 1,
      groupName: 'My Family',
      inviteCode: 'ABC123',
      createdAt: '2024-01-01T00:00:00Z',
    }),
    changePassword: jest.fn().mockResolvedValue({ accessToken: 'tok', refreshToken: 'rtok' }),
    leaveGroup: jest.fn().mockResolvedValue({ accessToken: 'new-tok', refreshToken: 'new-rtok' }),
  };

  window.confirm = jest.fn().mockReturnValue(true);

  // Mock localStorage
  const storage = {};
  jest.spyOn(Storage.prototype, 'setItem').mockImplementation((k, v) => { storage[k] = v; });
  jest.spyOn(Storage.prototype, 'removeItem').mockImplementation((k) => { delete storage[k]; });

  // Mock window.location
  delete window.location;
  window.location = { href: '', reload: jest.fn() };

  const module = require('../../pages/myPage');
  renderMyPage = module.renderMyPage || global.renderMyPage;

  document.body.innerHTML = '<div id="container"></div>';
});

afterEach(() => {
  jest.restoreAllMocks();
  delete global.ProfileApi;
});

describe('renderMyPage', () => {
  test('コンテナにマイページを描画する', async () => {
    const container = document.getElementById('container');

    renderMyPage(container);
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(container.querySelector('.mypage')).not.toBeNull();
  });

  test('プロフィール情報を読み込む', async () => {
    const container = document.getElementById('container');

    renderMyPage(container);
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(global.ProfileApi.getProfile).toHaveBeenCalled();
  });

  test('メールアドレスを表示する（XSS安全）', async () => {
    global.ProfileApi.getProfile.mockResolvedValue({
      id: 'u1',
      email: '<script>xss</script>@test.com',
      role: 'User',
      familyGroupId: 1,
      groupName: 'Group',
      inviteCode: 'INV',
    });
    const container = document.getElementById('container');

    renderMyPage(container);
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(container.innerHTML).not.toContain('<script>xss</script>');
    expect(container.textContent).toContain('<script>xss</script>@test.com');
  });

  test('招待コードを表示する', async () => {
    const container = document.getElementById('container');

    renderMyPage(container);
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(container.textContent).toContain('ABC123');
  });

  test('パスワード変更フォームが存在する', async () => {
    const container = document.getElementById('container');

    renderMyPage(container);
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(container.querySelector('form.password-change-form')).not.toBeNull();
  });

  test('パスワード変更フォームに現在・新・確認パスワードフィールドがある', async () => {
    const container = document.getElementById('container');

    renderMyPage(container);
    await new Promise((resolve) => setTimeout(resolve, 0));

    const form = container.querySelector('form.password-change-form');
    expect(form.querySelector('[name="currentPassword"]')).not.toBeNull();
    expect(form.querySelector('[name="newPassword"]')).not.toBeNull();
    expect(form.querySelector('[name="confirmPassword"]')).not.toBeNull();
  });

  test('新パスワードと確認パスワードが一致しない場合エラーを表示する', async () => {
    const container = document.getElementById('container');

    renderMyPage(container);
    await new Promise((resolve) => setTimeout(resolve, 0));

    const form = container.querySelector('form.password-change-form');
    form.querySelector('[name="currentPassword"]').value = 'OldPass1';
    form.querySelector('[name="newPassword"]').value = 'NewPass1';
    form.querySelector('[name="confirmPassword"]').value = 'Different1';

    form.dispatchEvent(new Event('submit'));

    expect(global.ProfileApi.changePassword).not.toHaveBeenCalled();
  });

  test('グループ脱退ボタンが存在する（グループ所属時）', async () => {
    const container = document.getElementById('container');

    renderMyPage(container);
    await new Promise((resolve) => setTimeout(resolve, 0));

    const leaveBtn = container.querySelector('.leave-group-btn');
    expect(leaveBtn).not.toBeNull();
  });

  test('グループ脱退確認ダイアログを表示する', async () => {
    const container = document.getElementById('container');

    renderMyPage(container);
    await new Promise((resolve) => setTimeout(resolve, 0));

    const leaveBtn = container.querySelector('.leave-group-btn');
    leaveBtn.click();

    expect(window.confirm).toHaveBeenCalled();
  });
});
