'use strict';

/**
 * MenuCraft - Main application entry point.
 * Checks authentication and redirects to login if needed.
 */
(function initApp() {
  const token = localStorage.getItem('authToken');

  if (!token) {
    window.location.href = '/login.html';
    return;
  }

  const main = document.getElementById('app-main');
  if (main) {
    main.textContent = 'MenuCraft へようこそ！';
  }
})();
