'use strict';
/**
 * Tests for AdminApi module (src/client/js/api/admin.js)
 * @jest-environment jsdom
 */

let AdminApi;

beforeEach(() => {
  jest.resetModules();

  // Mock apiFetch global
  global.apiFetch = jest.fn();

  const module = require('../../api/admin');
  AdminApi = module.AdminApi || global.AdminApi;
});

afterEach(() => {
  jest.restoreAllMocks();
  delete global.apiFetch;
});

describe('AdminApi.getUsers', () => {
  test('GET /api/admin/users を呼び出す', async () => {
    global.apiFetch.mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue({ success: true, data: [], error: null }),
    });

    await AdminApi.getUsers();

    expect(global.apiFetch).toHaveBeenCalledWith('/api/admin/users');
  });

  test('ユーザー一覧を返す', async () => {
    const users = [{ id: '1', email: 'admin@example.com', role: 'Admin' }];
    global.apiFetch.mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue({ success: true, data: users, error: null }),
    });

    const result = await AdminApi.getUsers();

    expect(result).toEqual(users);
  });

  test('HTTPエラー時に例外をスローする', async () => {
    global.apiFetch.mockResolvedValue({ ok: false, status: 403 });

    await expect(AdminApi.getUsers()).rejects.toThrow('HTTP 403');
  });
});

describe('AdminApi.changeRole', () => {
  test('PUT /api/admin/users/{userId}/role を呼び出す', async () => {
    global.apiFetch.mockResolvedValue({ ok: true });

    await AdminApi.changeRole('user-123', 'User');

    expect(global.apiFetch).toHaveBeenCalledWith(
      '/api/admin/users/user-123/role',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({ role: 'User' }),
      })
    );
  });

  test('HTTPエラー時にエラーメッセージをスローする', async () => {
    global.apiFetch.mockResolvedValue({
      ok: false,
      json: jest.fn().mockResolvedValue({ success: false, data: null, error: '最後の管理者は降格できません' }),
    });

    await expect(AdminApi.changeRole('user-123', 'User')).rejects.toThrow('最後の管理者は降格できません');
  });
});

describe('AdminApi.getGroups', () => {
  test('GET /api/admin/groups を呼び出す', async () => {
    global.apiFetch.mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue({ success: true, data: [], error: null }),
    });

    await AdminApi.getGroups();

    expect(global.apiFetch).toHaveBeenCalledWith('/api/admin/groups');
  });

  test('グループ一覧を返す', async () => {
    const groups = [{ id: 1, name: 'Family', inviteCode: 'ABC123' }];
    global.apiFetch.mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue({ success: true, data: groups, error: null }),
    });

    const result = await AdminApi.getGroups();

    expect(result).toEqual(groups);
  });

  test('HTTPエラー時に例外をスローする', async () => {
    global.apiFetch.mockResolvedValue({ ok: false, status: 403 });

    await expect(AdminApi.getGroups()).rejects.toThrow('HTTP 403');
  });
});

describe('AdminApi.updateGroup', () => {
  test('PUT /api/admin/groups/{groupId} を呼び出す', async () => {
    global.apiFetch.mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue({}),
    });

    await AdminApi.updateGroup(1, 'New Name');

    expect(global.apiFetch).toHaveBeenCalledWith(
      '/api/admin/groups/1',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({ name: 'New Name' }),
      })
    );
  });
});

describe('AdminApi.deleteGroup', () => {
  test('DELETE /api/admin/groups/{groupId} を呼び出す', async () => {
    global.apiFetch.mockResolvedValue({ ok: true });

    await AdminApi.deleteGroup(1);

    expect(global.apiFetch).toHaveBeenCalledWith(
      '/api/admin/groups/1',
      expect.objectContaining({ method: 'DELETE' })
    );
  });
});

describe('AdminApi.removeMember', () => {
  test('DELETE /api/admin/groups/{groupId}/members/{userId} を呼び出す', async () => {
    global.apiFetch.mockResolvedValue({ ok: true });

    await AdminApi.removeMember(1, 'user-456');

    expect(global.apiFetch).toHaveBeenCalledWith(
      '/api/admin/groups/1/members/user-456',
      expect.objectContaining({ method: 'DELETE' })
    );
  });
});

describe('AdminApi.regenerateInviteCode', () => {
  test('POST /api/admin/groups/{groupId}/invite-code を呼び出す', async () => {
    const groupData = { id: 1, inviteCode: 'NEW456', name: 'Family', memberCount: 2, createdAt: '2024-01-01T00:00:00Z' };
    global.apiFetch.mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue({ success: true, data: groupData, error: null }),
    });

    const result = await AdminApi.regenerateInviteCode(1);

    expect(global.apiFetch).toHaveBeenCalledWith(
      '/api/admin/groups/1/invite-code',
      expect.objectContaining({ method: 'POST' })
    );
    expect(result).toEqual(groupData);
  });

  test('HTTPエラー時に例外をスローする', async () => {
    global.apiFetch.mockResolvedValue({ ok: false, status: 404 });

    await expect(AdminApi.regenerateInviteCode(999)).rejects.toThrow('HTTP 404');
  });
});
