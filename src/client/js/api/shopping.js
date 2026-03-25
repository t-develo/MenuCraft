'use strict';

/**
 * Shopping list API client.
 */
const ShoppingApi = {
  /**
   * Get shopping list for the given week.
   * @param {string} weekStart - ISO date string (e.g. "2024-01-15"), must be a Monday
   * @returns {Promise<{weekStart: string, items: Array}>}
   */
  async getList(weekStart) {
    const res = await apiFetch(`/api/shopping-list?weekStart=${encodeURIComponent(weekStart)}`);
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.error || '買い物リストの取得に失敗しました');
    }
    const data = await res.json();
    return data.data;
  },

  /**
   * Update the checked state of a shopping item.
   * @param {string} weekStart - ISO date string (e.g. "2024-01-15")
   * @param {string} ingredientName - Name of the ingredient
   * @param {boolean} isChecked - New checked state
   * @returns {Promise<void>}
   */
  async updateCheck(weekStart, ingredientName, isChecked) {
    const res = await apiFetch('/api/shopping-list/check', {
      method: 'PUT',
      body: JSON.stringify({ weekStart, ingredientName, isChecked }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.error || 'チェック状態の更新に失敗しました');
    }
  },
};
