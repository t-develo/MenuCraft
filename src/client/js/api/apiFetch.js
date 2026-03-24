'use strict';

/**
 * Central HTTP client for MenuCraft API.
 * Attaches Bearer token and handles 401 responses.
 *
 * @param {string} url - API endpoint path (e.g., '/api/recipes')
 * @param {RequestInit} [options={}] - Fetch options
 * @returns {Promise<Response>}
 */
async function apiFetch(url, options = {}) {
  const token = localStorage.getItem('authToken');

  const headers = {
    'Content-Type': 'application/json',
    ...(token && url.startsWith('/api/') ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (response.status === 401) {
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    window.location.href = '/login.html';
    return response;
  }

  return response;
}
