'use strict';
/**
 * Tests for apiFetch.js - 401 auto-refresh logic
 * @jest-environment jsdom
 */

// We load the module via a helper that provides the globals it needs
let apiFetch;
let redirectedUrl;

beforeEach(() => {
  // Reset modules so _isRefreshing flag resets between tests
  jest.resetModules();

  // Mock localStorage
  const storage = {};
  jest.spyOn(Storage.prototype, 'getItem').mockImplementation((key) => storage[key] ?? null);
  jest.spyOn(Storage.prototype, 'setItem').mockImplementation((key, val) => { storage[key] = val; });
  jest.spyOn(Storage.prototype, 'removeItem').mockImplementation((key) => { delete storage[key]; });

  // Set tokens
  localStorage.setItem('authToken', 'valid-access-token');
  localStorage.setItem('refreshToken', 'valid-refresh-token');

  // Mock AppConfig global
  global.AppConfig = { API_BASE_URL: '' };

  // Load apiFetch module
  const mod = require('../../api/apiFetch');
  apiFetch = mod.apiFetch;

  // Inject redirect mock to avoid jsdom navigation issues
  redirectedUrl = null;
  mod._setRedirect((url) => { redirectedUrl = url; });
});

afterEach(() => {
  jest.restoreAllMocks();
  delete global.AppConfig;
});

describe('apiFetch - 通常リクエスト', () => {
  test('Authorization ヘッダーに Bearer トークンを付与する', async () => {
    global.fetch = jest.fn().mockResolvedValue({
      status: 200,
      ok: true,
    });

    await apiFetch('/api/recipes');

    expect(global.fetch).toHaveBeenCalledWith(
      '/api/recipes',
      expect.objectContaining({
        headers: expect.objectContaining({
          Authorization: 'Bearer valid-access-token',
        }),
      })
    );
  });

  test('Content-Type: application/json を付与する', async () => {
    global.fetch = jest.fn().mockResolvedValue({ status: 200, ok: true });

    await apiFetch('/api/recipes');

    expect(global.fetch).toHaveBeenCalledWith(
      '/api/recipes',
      expect.objectContaining({
        headers: expect.objectContaining({
          'Content-Type': 'application/json',
        }),
      })
    );
  });

  test('200 レスポンスをそのまま返す', async () => {
    const mockResponse = { status: 200, ok: true };
    global.fetch = jest.fn().mockResolvedValue(mockResponse);

    const result = await apiFetch('/api/recipes');

    expect(result).toBe(mockResponse);
  });
});

describe('apiFetch - 401 自動リフレッシュ', () => {
  test('401 受信時にリフレッシュエンドポイントを呼び出す', async () => {
    const refreshResponse = {
      status: 200,
      ok: true,
      json: jest.fn().mockResolvedValue({
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
      }),
    };
    const retryResponse = { status: 200, ok: true };

    global.fetch = jest.fn()
      .mockResolvedValueOnce({ status: 401, ok: false })   // original request fails
      .mockResolvedValueOnce(refreshResponse)               // refresh succeeds
      .mockResolvedValueOnce(retryResponse);                // retry succeeds

    await apiFetch('/api/recipes');

    expect(global.fetch).toHaveBeenCalledTimes(3);
    expect(global.fetch).toHaveBeenNthCalledWith(
      2,
      '/api/auth/refresh',
      expect.objectContaining({ method: 'POST' })
    );
  });

  test('リフレッシュ成功後に新しいトークンを保存する', async () => {
    const refreshResponse = {
      status: 200,
      ok: true,
      json: jest.fn().mockResolvedValue({
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
      }),
    };

    global.fetch = jest.fn()
      .mockResolvedValueOnce({ status: 401, ok: false })
      .mockResolvedValueOnce(refreshResponse)
      .mockResolvedValueOnce({ status: 200, ok: true });

    await apiFetch('/api/recipes');

    expect(localStorage.setItem).toHaveBeenCalledWith('authToken', 'new-access-token');
    expect(localStorage.setItem).toHaveBeenCalledWith('refreshToken', 'new-refresh-token');
  });

  test('リフレッシュ成功後に元のリクエストをリトライする', async () => {
    const refreshResponse = {
      status: 200,
      ok: true,
      json: jest.fn().mockResolvedValue({
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
      }),
    };
    const retryResponse = { status: 200, ok: true };

    global.fetch = jest.fn()
      .mockResolvedValueOnce({ status: 401, ok: false })
      .mockResolvedValueOnce(refreshResponse)
      .mockResolvedValueOnce(retryResponse);

    const result = await apiFetch('/api/recipes');

    // 3rd call is the retry
    expect(global.fetch).toHaveBeenCalledTimes(3);
    expect(result).toBe(retryResponse);
  });

  test('リフレッシュ失敗時にトークンを削除してログインページへリダイレクト', async () => {
    global.fetch = jest.fn()
      .mockResolvedValueOnce({ status: 401, ok: false })  // original
      .mockResolvedValueOnce({ status: 401, ok: false }); // refresh also fails

    await apiFetch('/api/recipes');

    expect(localStorage.removeItem).toHaveBeenCalledWith('authToken');
    expect(localStorage.removeItem).toHaveBeenCalledWith('refreshToken');
    expect(redirectedUrl).toBe('/login.html');
  });

  test('無限ループ防止: リフレッシュ API 自体の 401 はリダイレクトのみ行う', async () => {
    global.fetch = jest.fn()
      .mockResolvedValue({ status: 401, ok: false });

    await apiFetch('/api/auth/refresh');

    // refresh endpoint itself should not trigger another refresh attempt
    expect(global.fetch).toHaveBeenCalledTimes(1);
    expect(redirectedUrl).toBe('/login.html');
  });

  test('ログインエンドポイントの 401 はリフレッシュを試みない', async () => {
    global.fetch = jest.fn()
      .mockResolvedValue({ status: 401, ok: false });

    await apiFetch('/api/auth/login');

    // Should not call refresh
    expect(global.fetch).toHaveBeenCalledTimes(1);
  });

  test('登録エンドポイントの 401 はリフレッシュを試みない', async () => {
    global.fetch = jest.fn()
      .mockResolvedValue({ status: 401, ok: false });

    await apiFetch('/api/auth/register');

    expect(global.fetch).toHaveBeenCalledTimes(1);
  });
});
