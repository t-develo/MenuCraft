'use strict';

/**
 * Profile API module for MenuCraft.
 * Provides methods for current user's profile, password change, and group leave.
 */
const ProfileApi = {
  /**
   * Get the current user's profile.
   * @returns {Promise<object>}
   */
  async getProfile() {
    const res = await apiFetch('/api/profile');
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const body = await res.json();
    return body.data;
  },

  /**
   * Change the current user's password.
   * @param {string} currentPassword
   * @param {string} newPassword
   * @returns {Promise<{accessToken: string, refreshToken: string}>}
   */
  async changePassword(currentPassword, newPassword) {
    const res = await apiFetch('/api/profile/password', {
      method: 'PUT',
      body: JSON.stringify({ currentPassword, newPassword }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.error || 'パスワードの変更に失敗しました');
    }
    const body = await res.json();
    return body.data;
  },

  /**
   * Leave the current group.
   * @returns {Promise<{accessToken: string, refreshToken: string}>}
   */
  async leaveGroup() {
    const res = await apiFetch('/api/profile/leave-group', { method: 'POST' });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.error || 'グループの脱退に失敗しました');
    }
    const body = await res.json();
    return body.data;
  },
};

// Export for testing (CommonJS); in browser ProfileApi is a global
if (typeof module !== 'undefined' && module.exports) {
  module.exports = { ProfileApi };
}
