'use strict';

/**
 * Recipes API client.
 */
const RecipesApi = {
  async getAll() {
    const res = await apiFetch('/api/recipes');
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const data = await res.json();
    return data.data;
  },

  async getById(id) {
    const res = await apiFetch(`/api/recipes/${id}`);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const data = await res.json();
    return data.data;
  },

  async create(recipeData) {
    const res = await apiFetch('/api/recipes', {
      method: 'POST',
      body: JSON.stringify(recipeData),
    });
    if (!res.ok) {
      const err = await res.json();
      throw new Error(err.error || 'レシピの作成に失敗しました');
    }
    const data = await res.json();
    return data.data;
  },

  async update(id, recipeData) {
    const res = await apiFetch(`/api/recipes/${id}`, {
      method: 'PUT',
      body: JSON.stringify(recipeData),
    });
    if (!res.ok) {
      const err = await res.json();
      throw new Error(err.error || 'レシピの更新に失敗しました');
    }
    const data = await res.json();
    return data.data;
  },

  async delete(id) {
    const res = await apiFetch(`/api/recipes/${id}`, {
      method: 'DELETE',
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
  },
};
