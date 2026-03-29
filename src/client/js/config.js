'use strict';

/**
 * Application configuration.
 *
 * API_BASE_URL is injected at deploy time by the CI/CD pipeline.
 * The placeholder __API_BASE_URL__ is replaced with the actual
 * Azure Functions URL via `sed` in deploy-frontend.yml.
 *
 * For local development, set API_BASE_URL to the local Functions URL:
 *   http://localhost:7071
 */
const AppConfig = Object.freeze({
  API_BASE_URL: '__API_BASE_URL__',
});
