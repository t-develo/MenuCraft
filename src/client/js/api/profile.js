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
    return res.json();
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
    return res.json();
  },

  /**
   * Leave the current group.
   * @returns {Promise<{accessToken: string, refreshToken: string}>}
   */
  async leaveGroup() {
    const res = await apiFetch('/api/profile/leave-group', { method: 'POST' });
    return res.json();
  },
};

// Export for testing (CommonJS); in browser ProfileApi is a global
if (typeof module !== 'undefined' && module.exports) {
  module.exports = { ProfileApi };
}
