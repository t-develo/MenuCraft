'use strict';
/**
 * Tests for ProfileApi module (src/client/js/api/profile.js)
 * @jest-environment jsdom
 */

let ProfileApi;

beforeEach(() => {
  jest.resetModules();

  global.apiFetch = jest.fn();

  const module = require('../../api/profile');
  ProfileApi = module.ProfileApi || global.ProfileApi;
});

afterEach(() => {
  jest.restoreAllMocks();
  delete global.apiFetch;
});

describe('ProfileApi.getProfile', () => {
  test('GET /api/profile を呼び出す', async () => {
    global.apiFetch.mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue({}),
    });

    await ProfileApi.getProfile();

    expect(global.apiFetch).toHaveBeenCalledWith('/api/profile');
  });

  test('プロフィール情報を返す', async () => {
    const profile = {
      id: 'user-1',
      email: 'test@example.com',
      role: 'Admin',
      familyGroupId: 1,
      groupName: 'Family',
      inviteCode: 'ABC123',
    };
    global.apiFetch.mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue(profile),
    });

    const result = await ProfileApi.getProfile();

    expect(result).toEqual(profile);
  });
});

describe('ProfileApi.changePassword', () => {
  test('PUT /api/profile/password を呼び出す', async () => {
    global.apiFetch.mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue({}),
    });

    await ProfileApi.changePassword('oldPass123', 'newPass456');

    expect(global.apiFetch).toHaveBeenCalledWith(
      '/api/profile/password',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({ currentPassword: 'oldPass123', newPassword: 'newPass456' }),
      })
    );
  });

  test('新しいトークン情報を返す', async () => {
    const tokens = { accessToken: 'new-token', refreshToken: 'new-refresh' };
    global.apiFetch.mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue(tokens),
    });

    const result = await ProfileApi.changePassword('old', 'new');

    expect(result).toEqual(tokens);
  });
});

describe('ProfileApi.leaveGroup', () => {
  test('POST /api/profile/leave-group を呼び出す', async () => {
    global.apiFetch.mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue({}),
    });

    await ProfileApi.leaveGroup();

    expect(global.apiFetch).toHaveBeenCalledWith(
      '/api/profile/leave-group',
      expect.objectContaining({ method: 'POST' })
    );
  });

  test('新しいアクセストークンを返す', async () => {
    const tokens = { accessToken: 'new-no-group-token', refreshToken: 'new-refresh' };
    global.apiFetch.mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue(tokens),
    });

    const result = await ProfileApi.leaveGroup();

    expect(result).toEqual(tokens);
  });
});
