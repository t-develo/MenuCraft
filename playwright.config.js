// @ts-check
'use strict';

const { defineConfig, devices } = require('@playwright/test');

/**
 * Playwright E2E test configuration for MenuCraft.
 * Tests run against a locally-served static build (or a running dev server).
 *
 * Set BASE_URL environment variable to point at a running instance.
 * Default: http://localhost:7071 (Azure Functions local emulator).
 */
module.exports = defineConfig({
  testDir: './tests/e2e',
  timeout: 30_000,
  retries: 1,
  workers: 1,

  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:7071',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    trace: 'on-first-retry',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
