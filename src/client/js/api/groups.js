'use strict';

/**
 * Groups API client.
 */
const GroupsApi = {
  /**
   * Create a new family group.
   * On success, saves the refreshed access token (which includes familyGroupId).
   * @param {string} name
   * @returns {Promise<{success: boolean, error: string|null}>}
   */
  async create(name) {
    const response = await apiFetch('/api/groups', {
      method: 'POST',
      body: JSON.stringify({ name }),
    });
    if (!response) return { success: false, error: 'ネットワークエラーが発生しました' };

    const data = await response.json();
    if (data.success && data.data && data.data.accessToken) {
      localStorage.setItem('authToken', data.data.accessToken);
    }
    return data;
  },

  /**
   * Join a group via invite code.
   * On success, saves the refreshed access token (which includes familyGroupId).
   * @param {string} inviteCode
   * @returns {Promise<{success: boolean, error: string|null}>}
   */
  async join(inviteCode) {
    const response = await apiFetch('/api/groups/join', {
      method: 'POST',
      body: JSON.stringify({ inviteCode }),
    });
    if (!response) return { success: false, error: 'ネットワークエラーが発生しました' };

    const data = await response.json();
    if (data.success && data.data && data.data.accessToken) {
      localStorage.setItem('authToken', data.data.accessToken);
    }
    return data;
  },

  /**
   * Get members of the current user's family group.
   * @returns {Promise<object>}
   */
  async getMembers() {
    const response = await apiFetch('/api/groups/members');
    if (!response) return null;
    return response.json();
  },
};
