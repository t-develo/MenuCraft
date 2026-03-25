'use strict';

// @ts-check
const { test, expect } = require('@playwright/test');
const { registerUser, createGroup, setAuthToken } = require('./helpers/auth');

test.describe('献立 → 買い物リスト フロー', () => {
  let tokenWithGroup;

  test.beforeEach(async ({ request }) => {
    const { accessToken } = await registerUser(request);
    const result = await createGroup(request, accessToken);
    tokenWithGroup = result.accessToken;
  });

  test('献立ボードが表示される', async ({ page }) => {
    await setAuthToken(page, tokenWithGroup);
    await page.goto('/');

    // Default page is meal plan board
    await expect(page.locator('h2:has-text("献立ボード")')).toBeVisible();
    // Week navigation should be present
    await expect(page.locator('button:has-text("← 前週")')).toBeVisible();
    await expect(page.locator('button:has-text("翌週 →")')).toBeVisible();
  });

  test('買い物リストページが表示される', async ({ page }) => {
    await setAuthToken(page, tokenWithGroup);
    await page.goto('/');

    await page.click('button:has-text("買い物リスト")');
    await expect(page.locator('h2:has-text("買い物リスト")')).toBeVisible();
    // Week navigation
    await expect(page.locator('button:has-text("← 前週")')).toBeVisible();
  });

  test('献立を作成して買い物リストに反映される', async ({ page, request }) => {
    // 1. Create a recipe with an ingredient
    const recipeRes = await request.post('/api/recipes', {
      data: {
        title: '買い物テスト用パスタ',
        ingredients: [{ name: 'スパゲッティ', quantity: '200', unit: 'g' }],
        tags: [],
      },
      headers: { Authorization: `Bearer ${tokenWithGroup}` },
    });
    expect(recipeRes.ok()).toBeTruthy();
    const recipeBody = await recipeRes.json();
    const recipeId = recipeBody.data.id;

    // 2. Add the recipe to Monday lunch of the current week
    const monday = getMondayOfCurrentWeek();
    const weekStart = formatDate(monday);

    const mealRes = await request.put(`/api/mealplans/${weekStart}/Lunch`, {
      data: { recipeIds: [recipeId] },
      headers: { Authorization: `Bearer ${tokenWithGroup}` },
    });
    expect(mealRes.ok()).toBeTruthy();

    // 3. Navigate to shopping list and verify the ingredient appears
    await setAuthToken(page, tokenWithGroup);
    await page.goto('/');
    await page.click('button:has-text("買い物リスト")');

    // Navigate to the same week
    // (Default shows current week so this should work immediately)
    await page.waitForSelector('.shopping-list, .empty-message', { timeout: 10_000 });
    await expect(page.locator('text=スパゲッティ')).toBeVisible();
  });
});

/** @returns {Date} */
function getMondayOfCurrentWeek() {
  const d = new Date();
  const day = d.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  d.setDate(d.getDate() + diff);
  d.setHours(0, 0, 0, 0);
  return d;
}

/** @param {Date} date @returns {string} */
function formatDate(date) {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}
