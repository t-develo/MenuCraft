---
description: Generate and run end-to-end tests with Playwright. Creates test journeys, runs tests, captures screenshots/videos/traces, and uploads artifacts.
---

# E2E Command

This command invokes the **e2e-runner** agent to generate, maintain, and execute end-to-end tests using Playwright.

## What This Command Does

1. **Generate Test Journeys** - Create Playwright tests for user flows
2. **Run E2E Tests** - Execute tests across browsers
3. **Capture Artifacts** - Screenshots, videos, traces on failures
4. **Upload Results** - HTML reports and JUnit XML
5. **Identify Flaky Tests** - Quarantine unstable tests

## When to Use

Use `/e2e` when:
- Testing critical user journeys (login, recipe registration, meal planning)
- Verifying multi-step flows work end-to-end
- Testing UI interactions and navigation
- Validating integration between frontend and backend
- Preparing for production deployment

## How It Works

The e2e-runner agent will:

1. **Analyze user flow** and identify test scenarios
2. **Generate Playwright test** using Page Object Model pattern
3. **Run tests** across multiple browsers (Chrome, Firefox, Safari)
4. **Capture failures** with screenshots, videos, and traces
5. **Generate report** with results and artifacts
6. **Identify flaky tests** and recommend fixes

## MenuCraft Critical Flows

**HIGH priority (must always pass):**
1. User can register and log in
2. User can add a recipe via URL (OGP fetch)
3. User can view and edit weekly meal plan
4. User can view shopping list generated from meal plan
5. User can check/uncheck shopping list items

**MEDIUM priority:**
1. Auto-generate meal plan
2. Recipe search and filter
3. Group invite and join flow

## Test Artifacts

When tests run, the following artifacts are captured:

**On All Tests:**
- HTML Report with timeline and results
- JUnit XML for CI integration

**On Failure Only:**
- Screenshot of the failing state
- Video recording of the test
- Trace file for debugging (step-by-step replay)
- Network logs
- Console logs

## Viewing Artifacts

```bash
# View HTML report in browser
npx playwright show-report

# View specific trace file
npx playwright show-trace artifacts/trace-abc123.zip

# Screenshots are saved in artifacts/ directory
```

## Flaky Test Detection

If a test fails intermittently:

```
FLAKY TEST DETECTED: tests/e2e/mealplan/save.spec.js

Recommended fixes:
1. Add explicit wait: await page.waitForSelector('[data-testid="save-btn"]')
2. Increase timeout: { timeout: 10000 }
3. Check for race conditions in API response
4. Verify element is not hidden by animation

Quarantine recommendation: Mark as test.fixme() until fixed
```

## CI/CD Integration

Add to your CI pipeline:

```yaml
# .github/workflows/e2e.yml
- name: Install Playwright
  run: npx playwright install --with-deps

- name: Run E2E tests
  run: npx playwright test

- name: Upload artifacts
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: playwright-report
    path: playwright-report/
```

## Best Practices

**DO:**
- Use Page Object Model for maintainability
- Use data-testid attributes for selectors
- Wait for API responses, not arbitrary timeouts
- Test critical user journeys end-to-end
- Run tests before merging to main
- Review artifacts when tests fail

**DON'T:**
- Use brittle selectors (CSS classes can change)
- Test implementation details
- Run tests against production
- Ignore flaky tests
- Skip artifact review on failures
- Test every edge case with E2E (use unit tests)

## Quick Commands

```bash
# Run all E2E tests
npx playwright test

# Run specific test file
npx playwright test tests/e2e/auth/login.spec.js

# Run in headed mode (see browser)
npx playwright test --headed

# Debug test
npx playwright test --debug

# Generate test code
npx playwright codegen http://localhost:4200

# View report
npx playwright show-report
```

## Related Agents

This command invokes the `e2e-runner` agent.

For manual installs, the source file lives at:
`.claude/agents/e2e-runner.md`
