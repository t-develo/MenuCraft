'use strict';

/**
 * Application configuration.
 *
 * API_BASE_URL is empty for local development (same-origin proxy)
 * and set to the Azure Functions URL in production.
 *
 * Override by setting window.__ENV__.API_BASE_URL before this script loads,
 * or by editing this file for production deployments.
 */
const AppConfig = Object.freeze({
  API_BASE_URL: (window.__ENV__ && window.__ENV__.API_BASE_URL) || '',
});
