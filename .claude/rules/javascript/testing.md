---
paths:
  - "**/*.js"
  - "src/client/**"
---
# Vanilla JavaScript Testing

> This file extends [common/testing.md](../common/testing.md) with Vanilla JS specific content.

## Testing Approach

Since this is a Vanilla JS SPA with no build step, testing uses lightweight approaches:

- **Unit tests**: Plain Jest (no transpilation needed for modern JS)
- **Integration tests**: Jest with `jsdom` for DOM testing
- **E2E tests**: Playwright for full browser testing

## Unit Testing with Jest

```bash
# Install Jest for client-side unit tests
npm install --save-dev jest jest-environment-jsdom

# Run tests
npx jest src/client/

# Run with coverage
npx jest src/client/ --coverage
```

## Test File Organization

```
src/client/
├── js/
│   ├── api/
│   │   └── recipes.js
│   ├── utils/
│   │   └── validation.js
│   └── components/
│       └── recipeCard.js
└── js/__tests__/
    ├── api/
    │   └── recipes.test.js
    ├── utils/
    │   └── validation.test.js
    └── components/
        └── recipeCard.test.js
```

## Example Unit Test

```javascript
// js/__tests__/utils/validation.test.js
const { validateRecipeForm, isValidUrl } = require('../../utils/validation');

describe('validateRecipeForm', () => {
  test('returns error when title is empty', () => {
    const errors = validateRecipeForm({ title: '', url: null });
    expect(errors).toContain('タイトルは必須です');
  });

  test('returns no errors for valid data', () => {
    const errors = validateRecipeForm({
      title: 'カレーライス',
      url: 'https://example.com/recipe',
    });
    expect(errors).toHaveLength(0);
  });

  test('returns error for invalid URL', () => {
    const errors = validateRecipeForm({
      title: 'カレーライス',
      url: 'not-a-url',
    });
    expect(errors.some(e => e.includes('URL'))).toBe(true);
  });
});

describe('isValidUrl', () => {
  test.each([
    ['https://example.com', true],
    ['http://example.com/path?q=1', true],
    ['not-a-url', false],
    ['', false],
    [null, false],
  ])('isValidUrl(%s) === %s', (input, expected) => {
    expect(isValidUrl(input)).toBe(expected);
  });
});
```

## DOM Testing with jsdom

```javascript
// js/__tests__/components/recipeCard.test.js
/**
 * @jest-environment jsdom
 */
const { createRecipeCard } = require('../../components/recipeCard');

describe('createRecipeCard', () => {
  test('renders recipe title safely', () => {
    const recipe = { id: 1, title: '<script>alert(1)</script>Pasta' };
    const card = createRecipeCard(recipe);

    // Title should be text, not executed HTML
    expect(card.querySelector('h3').textContent).toBe(recipe.title);
    expect(card.innerHTML).not.toContain('<script>');
  });

  test('calls onSelect when card is clicked', () => {
    const recipe = { id: 1, title: 'Pasta' };
    const onSelect = jest.fn();
    const card = createRecipeCard(recipe, { onSelect });

    card.click();
    expect(onSelect).toHaveBeenCalledWith(recipe);
  });
});
```

## E2E Testing

Use **Playwright** for critical user flows. See agent: `agents/e2e-runner.md`.

```bash
# Run E2E tests
npx playwright test

# Run specific test
npx playwright test tests/e2e/auth/login.spec.js
```

## Agent Support

- **e2e-runner** — Playwright E2E testing specialist
- **tdd-guide** — TDD methodology guidance (adapted for xUnit on backend)
