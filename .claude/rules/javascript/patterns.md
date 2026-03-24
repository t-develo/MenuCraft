---
paths:
  - "**/*.js"
  - "src/client/**"
---
# Vanilla JavaScript Patterns

> This file extends [common/patterns.md](../common/patterns.md) with Vanilla JS specific content.

## API Response Format

```javascript
// Expected API response envelope
// { success: boolean, data: T | null, error: string | null }

async function apiFetch(url, options = {}) {
  const token = localStorage.getItem('authToken');
  const response = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  });

  if (response.status === 401) {
    // Token expired — redirect to login
    window.location.href = '/login.html';
    return;
  }

  return response;
}
```

## Module Pattern

Organize code into logical modules using plain JS objects or ES modules:

```javascript
// js/api/recipes.js
const RecipesApi = {
  async getAll() {
    const res = await apiFetch('/api/recipes');
    return res.json();
  },

  async create(recipeData) {
    const res = await apiFetch('/api/recipes', {
      method: 'POST',
      body: JSON.stringify(recipeData),
    });
    return res.json();
  },

  async delete(id) {
    await apiFetch(`/api/recipes/${id}`, { method: 'DELETE' });
  },
};
```

## State Management Pattern

For simple SPA state, use a plain JS state object with update functions:

```javascript
// js/state.js
let appState = {
  recipes: [],
  mealPlan: null,
  currentWeek: null,
  isLoading: false,
  error: null,
};

function updateState(patch) {
  appState = { ...appState, ...patch };
  renderApp(); // re-render after state change
}

function getState() {
  return appState;
}
```

## Component Pattern

For reusable UI pieces, use functions that return DOM elements:

```javascript
// js/components/recipeCard.js
function createRecipeCard(recipe, { onSelect, onDelete } = {}) {
  const card = document.createElement('div');
  card.className = 'recipe-card';
  card.dataset.recipeId = recipe.id;

  const title = document.createElement('h3');
  title.textContent = recipe.title;  // safe: textContent not innerHTML

  const deleteBtn = document.createElement('button');
  deleteBtn.textContent = '削除';
  deleteBtn.addEventListener('click', (e) => {
    e.stopPropagation();
    onDelete?.(recipe.id);
  });

  card.addEventListener('click', () => onSelect?.(recipe));
  card.append(title, deleteBtn);

  return card;
}
```

## Event Delegation Pattern

Use event delegation for lists of dynamic items:

```javascript
// WRONG: Adding listeners to each item (memory leak if items are re-rendered)
recipes.forEach(recipe => {
  document.getElementById(`recipe-${recipe.id}`)
    .addEventListener('click', handler);
});

// CORRECT: Single listener on the parent
document.getElementById('recipe-list').addEventListener('click', (e) => {
  const card = e.target.closest('[data-recipe-id]');
  if (!card) return;
  const recipeId = parseInt(card.dataset.recipeId, 10);
  handleRecipeClick(recipeId);
});
```

## Debounce Pattern

For search inputs and other high-frequency events:

```javascript
function debounce(fn, delayMs) {
  let timeoutId;
  return function (...args) {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => fn.apply(this, args), delayMs);
  };
}

const handleSearch = debounce(async (query) => {
  const recipes = await RecipesApi.search(query);
  renderRecipeList(recipes);
}, 300);

searchInput.addEventListener('input', (e) => handleSearch(e.target.value));
```
