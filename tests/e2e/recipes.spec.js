'use strict';

// @ts-check
const { test, expect } = require('@playwright/test');
const { registerUser, createGroup, setAuthToken } = require('./helpers/auth');

test.describe('レシピ管理フロー', () => {
  let tokenWithGroup;

  test.beforeEach(async ({ request }) => {
    const { accessToken } = await registerUser(request);
    const result = await createGroup(request, accessToken);
    tokenWithGroup = result.accessToken;
  });

  test('レシピ一覧ページが表示される', async ({ page }) => {
    await setAuthToken(page, tokenWithGroup);
    await page.goto('/');

    // Navigate to recipes
    await page.click('button:has-text("レシピ")');

    await expect(page.locator('h2:has-text("レシピ")')).toBeVisible();
    await expect(page.locator('button:has-text("レシピを追加")')).toBeVisible();
  });

  test('レシピを追加して一覧に表示される', async ({ page, request }) => {
    // Create a recipe via API directly
    const response = await request.post('/api/recipes', {
      data: {
        title: 'テスト用カレーライス',
        description: 'テスト用のレシピ',
        tags: ['テスト'],
        ingredients: [],
      },
      headers: { Authorization: `Bearer ${tokenWithGroup}` },
    });
    expect(response.ok()).toBeTruthy();

    await setAuthToken(page, tokenWithGroup);
    await page.goto('/');
    await page.click('button:has-text("レシピ")');

    // Recipe should appear in the list
    await expect(page.locator('text=テスト用カレーライス')).toBeVisible();
  });
});
