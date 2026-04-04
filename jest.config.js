'use strict';

/** @type {import('jest').Config} */
module.exports = {
  testEnvironment: 'jest-environment-jsdom',
  testMatch: ['**/src/client/js/__tests__/**/*.test.js'],
  collectCoverageFrom: [
    'src/client/js/**/*.js',
    '!src/client/js/config.js',
  ],
  coverageThreshold: {
    global: {
      lines: 80,
    },
  },
};
