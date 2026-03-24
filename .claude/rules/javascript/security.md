---
paths:
  - "**/*.js"
  - "src/client/**"
---
# Vanilla JavaScript Security

> This file extends [common/security.md](../common/security.md) with Vanilla JS specific content.

## Secret Management

```javascript
// NEVER: Hardcoded secrets in client code
const apiKey = "sk-proj-xxxxx";  // This is visible to all users!

// CORRECT: Call your backend API which holds secrets server-side
// The client never sees API keys — only the Azure Function does
const response = await apiFetch('/api/recipes/fetch-ogp', {
  method: 'POST',
  body: JSON.stringify({ url }),
});
```

## XSS Prevention

```javascript
// NEVER: Setting innerHTML with user input
element.innerHTML = userInput;
listItem.innerHTML = `<div>${recipe.description}</div>`;

// ALWAYS: Use textContent for plain text
element.textContent = userInput;

// If HTML is truly needed (e.g., rendering markdown): use DOMPurify
import DOMPurify from 'dompurify';
element.innerHTML = DOMPurify.sanitize(htmlContent);
```

## Authentication Token Handling

```javascript
// Store JWT in localStorage (acceptable for this app's threat model)
// BUT: never log it, never send it to third-party domains
function saveAuthToken(token) {
  localStorage.setItem('authToken', token);
}

function getAuthToken() {
  return localStorage.getItem('authToken');
}

function clearAuthToken() {
  localStorage.removeItem('authToken');
  localStorage.removeItem('refreshToken');
}

// Always attach to same-origin API calls only
async function apiFetch(url, options = {}) {
  const token = getAuthToken();

  // IMPORTANT: Only attach token to our own API
  const headers = url.startsWith('/api/')
    ? { Authorization: `Bearer ${token}`, ...options.headers }
    : options.headers;

  return fetch(url, { ...options, headers });
}
```

## URL Validation

```javascript
// When handling user-provided URLs (e.g., recipe sources)
// send to backend for validation — never fetch directly from client

// WRONG: Client fetches user-provided URL directly
const response = await fetch(userProvidedUrl);  // SSRF risk via proxy

// CORRECT: Pass to backend which validates and fetches server-side
const response = await apiFetch('/api/recipes/fetch-ogp', {
  method: 'POST',
  body: JSON.stringify({ url: userProvidedUrl }),
});
```

## Input Sanitization

```javascript
// Always sanitize before inserting into DOM or sending to API
function sanitizeText(input) {
  if (typeof input !== 'string') return '';
  return input.trim().slice(0, 2000);  // limit length
}

// Validate form data before submission
function validateAndSubmit(formData) {
  const title = sanitizeText(formData.get('title'));
  if (!title) {
    showError('タイトルを入力してください');
    return;
  }
  // ... proceed with sanitized data
}
```

## Agent Support

- Use **security-reviewer** agent for comprehensive security audits
- Use **vanillajs-reviewer** agent for JS-specific security review
