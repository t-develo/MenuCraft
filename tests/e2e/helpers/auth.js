'use strict';

/**
 * Generate a unique email address for test isolation.
 * @returns {string}
 */
function uniqueEmail() {
  return `test_${Date.now()}_${Math.random().toString(36).slice(2, 7)}@example.com`;
}

/**
 * Register a new user via the API and return the access token.
 * @param {import('@playwright/test').APIRequestContext} request
 * @param {{ email?: string, password?: string }} opts
 * @returns {Promise<{ email: string, password: string, accessToken: string }>}
 */
async function registerUser(request, opts = {}) {
  const email = opts.email ?? uniqueEmail();
  const password = opts.password ?? 'TestPass1!';

  const response = await request.post('/api/auth/register', {
    data: { email, password },
  });

  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`Registration failed (${response.status()}): ${body}`);
  }

  const body = await response.json();
  return { email, password, accessToken: body.data.accessToken };
}

/**
 * Login via the API and return the access token.
 * @param {import('@playwright/test').APIRequestContext} request
 * @param {string} email
 * @param {string} password
 * @returns {Promise<string>}
 */
async function loginUser(request, email, password) {
  const response = await request.post('/api/auth/login', {
    data: { email, password },
  });

  if (!response.ok()) {
    throw new Error(`Login failed (${response.status()})`);
  }

  const body = await response.json();
  return body.data.accessToken;
}

/**
 * Create a family group via the API and return the updated access token (with familyGroupId).
 * @param {import('@playwright/test').APIRequestContext} request
 * @param {string} accessToken
 * @param {string} groupName
 * @returns {Promise<{ groupId: number, inviteCode: string, accessToken: string }>}
 */
async function createGroup(request, accessToken, groupName = 'Test Family') {
  const response = await request.post('/api/groups', {
    data: { name: groupName },
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`Group creation failed (${response.status()}): ${body}`);
  }

  const body = await response.json();
  return {
    groupId: body.data.groupId,
    inviteCode: body.data.inviteCode,
    accessToken: body.data.accessToken,
  };
}

/**
 * Inject an auth token into the browser's localStorage so the SPA sees the user as logged in.
 * @param {import('@playwright/test').Page} page
 * @param {string} accessToken
 */
async function setAuthToken(page, accessToken) {
  await page.addInitScript((token) => {
    localStorage.setItem('authToken', token);
  }, accessToken);
}

module.exports = { uniqueEmail, registerUser, loginUser, createGroup, setAuthToken };
