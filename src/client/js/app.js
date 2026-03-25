'use strict';

/**
 * MenuCraft - Main application entry point.
 * Handles authentication check, navigation, and page routing.
 */
(function initApp() {
  const token = localStorage.getItem('authToken');

  if (!token) {
    window.location.href = '/login.html';
    return;
  }

  const header = document.getElementById('app-header');
  const main = document.getElementById('app-main');

  if (!header || !main) return;

  // ── Navigation ─────────────────────────────────────────────────────────
  const nav = document.createElement('nav');
  nav.className = 'app-nav';

  const navItems = [
    { label: '献立ボード', page: 'mealplan' },
    { label: 'レシピ', page: 'recipes' },
  ];

  let activePage = 'mealplan';

  const navLinks = {};

  for (const item of navItems) {
    const link = document.createElement('button');
    link.className = 'nav-link';
    link.textContent = item.label;
    link.dataset.page = item.page;
    link.addEventListener('click', () => navigateTo(item.page));
    nav.appendChild(link);
    navLinks[item.page] = link;
  }

  const logoutBtn = document.createElement('button');
  logoutBtn.className = 'nav-link nav-logout';
  logoutBtn.textContent = 'ログアウト';
  logoutBtn.addEventListener('click', () => {
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    window.location.href = '/login.html';
  });
  nav.appendChild(logoutBtn);

  header.appendChild(nav);

  /**
   * Navigate to a page by key.
   * @param {string} page
   */
  function navigateTo(page) {
    activePage = page;

    // Update active nav link
    for (const [key, link] of Object.entries(navLinks)) {
      link.classList.toggle('active', key === page);
    }

    // Render the appropriate page
    if (page === 'mealplan') {
      renderMealPlanBoard(main);
    } else if (page === 'recipes') {
      renderRecipesPage(main);
    }
  }

  // Initial render
  navigateTo('mealplan');
})();
