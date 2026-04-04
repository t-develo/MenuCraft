'use strict';

/**
 * Central HTTP client for MenuCraft API.
 * Attaches Bearer token, handles 401 responses with automatic token refresh.
 * Supports cross-origin API calls via AppConfig.API_BASE_URL.
 *
 * @param {string} path - API endpoint path (e.g., '/api/recipes')
 * @param {RequestInit} [options={}] - Fetch options
 * @returns {Promise<Response>}
 */

let _isRefreshing = false;

/** Redirect function — overridable in tests */
let _redirect = (url) => {
  window.location.href = url;
};

async function apiFetch(path, options = {}) {
  const token = localStorage.getItem('authToken');
  const baseUrl = typeof AppConfig !== 'undefined' ? AppConfig.API_BASE_URL : '';
  const url = path.startsWith('/api/') ? `${baseUrl}${path}` : path;

  const headers = {
    'Content-Type': 'application/json',
    ...(token && path.startsWith('/api/') ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };

  const response = await fetch(url, {
    ...options,
    headers,
  });

  // Auth endpoints and the refresh endpoint itself should not trigger refresh
  const isAuthEndpoint =
    path === '/api/auth/login' ||
    path === '/api/auth/register' ||
    path === '/api/auth/refresh';

  if (response.status === 401 && !isAuthEndpoint && !_isRefreshing) {
    return _handleTokenRefresh(path, options);
  }

  // If the refresh endpoint itself returns 401, clear tokens and redirect
  if (response.status === 401 && (isAuthEndpoint || _isRefreshing)) {
    if (path !== '/api/auth/login' && path !== '/api/auth/register') {
      localStorage.removeItem('authToken');
      localStorage.removeItem('refreshToken');
      _redirect('/login.html');
    }
    return response;
  }

  return response;
}

/**
 * Attempt to refresh the access token using the stored refresh token.
 * On success, stores the new tokens and retries the original request.
 * On failure, clears tokens and redirects to login.
 *
 * @param {string} originalPath
 * @param {RequestInit} originalOptions
 * @returns {Promise<Response>}
 */
async function _handleTokenRefresh(originalPath, originalOptions) {
  _isRefreshing = true;
  try {
    const refreshToken = localStorage.getItem('refreshToken');
    const baseUrl = typeof AppConfig !== 'undefined' ? AppConfig.API_BASE_URL : '';
    const refreshUrl = `${baseUrl}/api/auth/refresh`;

    const refreshResponse = await fetch(refreshUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    });

    if (!refreshResponse.ok) {
      localStorage.removeItem('authToken');
      localStorage.removeItem('refreshToken');
      _redirect('/login.html');
      return refreshResponse;
    }

    const tokens = await refreshResponse.json();
    localStorage.setItem('authToken', tokens.accessToken);
    localStorage.setItem('refreshToken', tokens.refreshToken);

    // Retry the original request with new token
    return apiFetch(originalPath, originalOptions);
  } finally {
    _isRefreshing = false;
  }
}

// Export for testing (CommonJS); in browser these are globals
if (typeof module !== 'undefined' && module.exports) {
  module.exports = {
    apiFetch,
    _setRedirect: (fn) => { _redirect = fn; },
    _resetRefreshFlag: () => { _isRefreshing = false; },
  };
}
