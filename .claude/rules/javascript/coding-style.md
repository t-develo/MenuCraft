---
paths:
  - "**/*.js"
  - "src/client/**"
---
# Vanilla JavaScript Coding Style

> This file extends [common/coding-style.md](../common/coding-style.md) with Vanilla JS specific content.

## Guiding Principles

Write plain, readable JavaScript. No build step, no transpilation, no TypeScript. Keep it simple enough that any developer can open `index.html` and understand the code without tooling.

## Variable Declarations

- **`const`** by default for all declarations
- **`let`** only when reassignment is needed
- **Never `var`** — function-scoped variables cause subtle bugs

```javascript
// WRONG
var userId = 42;

// CORRECT
const userId = 42;

// CORRECT when reassignment is needed
let currentPage = 1;
currentPage++;
```

## Functions

- Prefer **arrow functions** for callbacks and short utilities
- Use **named function declarations** for top-level functions (easier debugging)
- Add JSDoc comments to public/exported functions

```javascript
// Top-level function: named declaration
async function fetchRecipes(familyGroupId) {
  const response = await apiFetch(`/api/recipes?groupId=${familyGroupId}`);
  return response.json();
}

// Callback: arrow function
const activeRecipes = recipes.filter(r => !r.isDeleted);
```

## Immutability

Use spread operators and array methods for immutable updates:

```javascript
// WRONG: Mutation
function updateRecipe(recipe, newTitle) {
  recipe.title = newTitle;  // mutation!
  return recipe;
}

// CORRECT: Immutability
function updateRecipe(recipe, newTitle) {
  return { ...recipe, title: newTitle };
}

// WRONG: Mutating array
recipes.push(newRecipe);

// CORRECT: Immutable append
const updatedRecipes = [...recipes, newRecipe];
```

## Error Handling

Always handle errors explicitly:

```javascript
// WRONG: Unhandled fetch rejection
fetch('/api/recipes').then(r => r.json()).then(updateUI);

// CORRECT: Explicit error handling
async function loadRecipes() {
  try {
    const response = await apiFetch('/api/recipes');
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    const data = await response.json();
    updateUI(data);
  } catch (error) {
    console.error('Failed to load recipes:', error);
    showErrorMessage('レシピの読み込みに失敗しました');
  }
}
```

## DOM Manipulation

- Prefer `textContent` over `innerHTML` for user-supplied content
- Use `createElement` + `appendChild` for dynamic HTML construction
- Use `DOMPurify.sanitize()` only when HTML rendering is truly necessary

```javascript
// WRONG: XSS risk
listItem.innerHTML = `<strong>${recipe.title}</strong>`;

// CORRECT: Safe text
const strong = document.createElement('strong');
strong.textContent = recipe.title;
listItem.appendChild(strong);
```

## Input Validation

Validate all user input and API responses before use:

```javascript
// Validate form inputs
function validateRecipeForm(data) {
  const errors = [];
  if (!data.title || data.title.trim() === '') {
    errors.push('タイトルは必須です');
  }
  if (data.url && !isValidUrl(data.url)) {
    errors.push('URLの形式が正しくありません');
  }
  return errors;
}

function isValidUrl(str) {
  try {
    new URL(str);
    return true;
  } catch {
    return false;
  }
}
```

## Console.log

- No `console.log` statements in production code
- Use `console.error` only for genuine error conditions in production
- Remove all debug `console.log` before merging

## Formatting

- 2 spaces for indentation
- Use Prettier if available for consistent formatting
- Semicolons required
- Single quotes for strings (or consistent double quotes — pick one)
