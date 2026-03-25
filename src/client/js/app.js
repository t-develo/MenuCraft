'use strict';

/**
 * MenuCraft - Main application entry point.
 * Handles authentication check, group membership check, navigation, and page routing.
 */

/**
 * Decode a JWT token payload without verifying the signature.
 * @param {string} token
 * @returns {object|null}
 */
function parseJwtPayload(token) {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
    return JSON.parse(atob(padded));
  } catch {
    return null;
  }
}

(function initApp() {
  const token = localStorage.getItem('authToken');

  if (!token) {
    window.location.href = '/login.html';
    return;
  }

  // Check if token is expired
  const payload = parseJwtPayload(token);
  if (!payload || (payload.exp && Date.now() / 1000 > payload.exp)) {
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    window.location.href = '/login.html';
    return;
  }

  // Check group membership — familyGroupId claim present in JWT
  const hasFamilyGroup = payload['familyGroupId'] !== undefined && payload['familyGroupId'] !== null;

  const header = document.getElementById('app-header');
  const main = document.getElementById('app-main');

  if (!header || !main) return;

  if (!hasFamilyGroup) {
    // Show group setup page without navigation
    renderGroupSetup(main);
    return;
  }

  // ── Navigation ─────────────────────────────────────────────────────────
  const nav = document.createElement('nav');
  nav.className = 'app-nav';
  nav.setAttribute('role', 'navigation');
  nav.setAttribute('aria-label', 'メインナビゲーション');

  // Logo / brand
  const brand = document.createElement('span');
  brand.className = 'nav-brand';
  brand.textContent = 'MenuCraft';
  nav.appendChild(brand);

  const navItems = [
    { label: '献立ボード', page: 'mealplan' },
    { label: 'レシピ', page: 'recipes' },
    { label: '買い物リスト', page: 'shopping' },
  ];

  let activePage = 'mealplan';

  const navLinks = {};

  for (const item of navItems) {
    const link = document.createElement('button');
    link.className = 'nav-link';
    link.textContent = item.label;
    link.dataset.page = item.page;
    link.setAttribute('aria-current', item.page === activePage ? 'page' : 'false');
    link.addEventListener('click', () => navigateTo(item.page));
    nav.appendChild(link);
    navLinks[item.page] = link;
  }

  const logoutBtn = document.createElement('button');
  logoutBtn.className = 'nav-link nav-logout';
  logoutBtn.textContent = 'ログアウト';
  logoutBtn.setAttribute('aria-label', 'ログアウト');
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
      const isActive = key === page;
      link.classList.toggle('active', isActive);
      link.setAttribute('aria-current', isActive ? 'page' : 'false');
    }

    // Render the appropriate page
    if (page === 'mealplan') {
      renderMealPlanBoard(main);
    } else if (page === 'recipes') {
      renderRecipesPage(main);
    } else if (page === 'shopping') {
      renderShoppingListPage(main);
    }
  }

  // Support URL hash-based routing on load
  const hashPage = window.location.hash.replace('#', '');
  if (hashPage && navLinks[hashPage]) {
    navigateTo(hashPage);
  } else {
    navigateTo('mealplan');
  }

  // Update hash when navigating
  window.addEventListener('hashchange', () => {
    const page = window.location.hash.replace('#', '');
    if (page && navLinks[page] && page !== activePage) {
      navigateTo(page);
    }
  });
})();
