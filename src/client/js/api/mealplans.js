'use strict';

/**
 * MealPlans API client.
 */
const MealPlansApi = {
  /**
   * Get weekly meal plan starting from the given Monday.
   * @param {string} weekStart - ISO date string (e.g. "2024-01-15"), must be a Monday
   * @returns {Promise<{weekStart: string, plans: Array}>}
   */
  async getWeekly(weekStart) {
    const res = await apiFetch(`/api/mealplans?weekStart=${encodeURIComponent(weekStart)}`);
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.error || '献立の取得に失敗しました');
    }
    const data = await res.json();
    return data.data;
  },

  /**
   * Update a single meal slot.
   * @param {string} date - ISO date string (e.g. "2024-01-15")
   * @param {string} mealType - "Lunch" or "Dinner"
   * @param {number[]} recipeIds - Array of recipe IDs to assign
   * @returns {Promise<{date: string, mealType: string, recipes: Array}>}
   */
  async update(date, mealType, recipeIds) {
    const res = await apiFetch(`/api/mealplans/${encodeURIComponent(date)}/${encodeURIComponent(mealType)}`, {
      method: 'PUT',
      body: JSON.stringify({ recipeIds }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.error || '献立の更新に失敗しました');
    }
    const data = await res.json();
    return data.data;
  },
};
