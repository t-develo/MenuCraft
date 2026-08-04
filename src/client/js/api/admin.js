'use strict';

/**
 * Admin API module for MenuCraft.
 * Provides methods to manage users and groups (Admin role only).
 */
const AdminApi = {
  /**
   * Get all users.
   * @returns {Promise<Array>}
   */
  async getUsers() {
    const res = await apiFetch('/api/management/users');
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const body = await res.json();
    return body.data;
  },

  /**
   * Change a user's role.
   * @param {string} userId
   * @param {string} role - 'Admin' or 'User'
   * @returns {Promise<void>}
   */
  async changeRole(userId, role) {
    const res = await apiFetch(`/api/management/users/${userId}/role`, {
      method: 'PUT',
      body: JSON.stringify({ role }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.error || 'ロールの変更に失敗しました');
    }
  },

  /**
   * Get all groups.
   * @returns {Promise<Array>}
   */
  async getGroups() {
    const res = await apiFetch('/api/management/groups');
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const body = await res.json();
    return body.data;
  },

  /**
   * Update a group's name.
   * @param {number} groupId
   * @param {string} name
   * @returns {Promise<object>}
   */
  async updateGroup(groupId, name) {
    const res = await apiFetch(`/api/management/groups/${groupId}`, {
      method: 'PUT',
      body: JSON.stringify({ name }),
    });
    return res.json();
  },

  /**
   * Delete a group.
   * @param {number} groupId
   * @returns {Promise<void>}
   */
  async deleteGroup(groupId) {
    await apiFetch(`/api/management/groups/${groupId}`, { method: 'DELETE' });
  },

  /**
   * Remove a member from a group.
   * @param {number} groupId
   * @param {string} userId
   * @returns {Promise<void>}
   */
  async removeMember(groupId, userId) {
    await apiFetch(`/api/management/groups/${groupId}/members/${userId}`, {
      method: 'DELETE',
    });
  },

  /**
   * Regenerate invite code for a group.
   * @param {number} groupId
   * @returns {Promise<{inviteCode: string}>}
   */
  async regenerateInviteCode(groupId) {
    const res = await apiFetch(`/api/management/groups/${groupId}/invite-code`, {
      method: 'POST',
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const body = await res.json();
    return body.data;
  },
};

// Export for testing (CommonJS); in browser AdminApi is a global
if (typeof module !== 'undefined' && module.exports) {
  module.exports = { AdminApi };
}
