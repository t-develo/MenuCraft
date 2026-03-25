'use strict';

// @ts-check
const { test, expect } = require('@playwright/test');
const { registerUser, loginUser, uniqueEmail } = require('./helpers/auth');

test.describe('認証フロー', () => {
  test('新規登録 → ログイン画面へリダイレクト', async ({ page, request }) => {
    // Register a new user and verify the API succeeds
    const { email, password, accessToken } = await registerUser(request);
    expect(accessToken).toBeTruthy();

    // Visit login page
    await page.goto('/login.html');
    await expect(page).toHaveTitle(/MenuCraft/);

    // Fill in login form with the newly registered credentials
    await page.fill('#email', email);
    await page.fill('#password', password);
    await page.click('button[type="submit"]');

    // After successful login the app should redirect to '/'
    await expect(page).toHaveURL('/');
  });

  test('無効な認証情報でログイン失敗', async ({ page }) => {
    await page.goto('/login.html');

    await page.fill('#email', 'nobody@example.com');
    await page.fill('#password', 'wrongpassword');
    await page.click('button[type="submit"]');

    // Should stay on login page and show error
    await expect(page).toHaveURL('/login.html');
    const errorDiv = page.locator('#login-error');
    await expect(errorDiv).toBeVisible();
    await expect(errorDiv).not.toBeEmpty();
  });

  test('未認証でルートアクセス → ログインページへリダイレクト', async ({ page }) => {
    // Clear storage and navigate to protected root
    await page.goto('/');
    await expect(page).toHaveURL('/login.html');
  });
});
