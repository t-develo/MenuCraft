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

  /**
   * Fetches OGP metadata from the given URL via the server.
   * @param {string} url
   * @returns {Promise<{title: string|null, imageUrl: string|null, description: string|null}>}
   */
  async fetchOgp(url) {
    const res = await apiFetch('/api/recipes/fetch-ogp', {
      method: 'POST',
      body: JSON.stringify({ url }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.error || 'OGP情報の取得に失敗しました');
    }
    const data = await res.json();
    return data.data;
  },

  /**
   * Parses ingredient text via the server.
   * @param {string} text - Multi-line ingredient text
   * @returns {Promise<Array<{name: string, quantity: string|null, unit: string|null}>>}
   */
  async parseIngredients(text) {
    const res = await apiFetch('/api/recipes/parse-ingredients', {
      method: 'POST',
      body: JSON.stringify({ text }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.error || '材料の解析に失敗しました');
    }
    const data = await res.json();
    return data.data.ingredients;
  },
};
