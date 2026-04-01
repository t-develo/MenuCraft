'use strict';

/**
 * Central HTTP client for MenuCraft API.
 * Attaches Bearer token and handles 401 responses.
 * Supports cross-origin API calls via AppConfig.API_BASE_URL.
 *
 * @param {string} path - API endpoint path (e.g., '/api/recipes')
 * @param {RequestInit} [options={}] - Fetch options
 * @returns {Promise<Response>}
 */
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

  // Skip the 401 redirect for auth endpoints — they handle errors themselves.
  const isAuthEndpoint = path === '/api/auth/login' || path === '/api/auth/register';
  if (response.status === 401 && !isAuthEndpoint) {
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    window.location.href = '/login.html';
    return response;
  }

  return response;
}
