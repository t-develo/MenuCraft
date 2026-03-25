'use strict';

// @ts-check
const { test, expect } = require('@playwright/test');
const { registerUser, createGroup, setAuthToken } = require('./helpers/auth');

test.describe('グループ設定フロー', () => {
  test('グループ未所属ユーザーはグループ設定画面が表示される', async ({ page, request }) => {
    const { accessToken } = await registerUser(request);

    // Inject token without familyGroupId (no group yet)
    await setAuthToken(page, accessToken);
    await page.goto('/');

    // Should render group setup instead of main nav
    await expect(page.locator('text=グループ設定')).toBeVisible();
    await expect(page.locator('text=新しいグループを作成')).toBeVisible();
    await expect(page.locator('text=招待コードで参加')).toBeVisible();
  });

  test('グループを作成するとメインアプリへ遷移する', async ({ page, request }) => {
    const { accessToken } = await registerUser(request);
    const { accessToken: tokenWithGroup } = await createGroup(request, accessToken);

    // Inject token that already has familyGroupId
    await setAuthToken(page, tokenWithGroup);
    await page.goto('/');

    // Navigation bar should be visible with main links
    await expect(page.locator('.app-nav')).toBeVisible();
    await expect(page.locator('text=献立ボード')).toBeVisible();
    await expect(page.locator('text=レシピ')).toBeVisible();
    await expect(page.locator('text=買い物リスト')).toBeVisible();
  });
});
