'use strict';

/**
 * Auth API client and form handlers.
 */

/**
 * Show error message on auth forms.
 * @param {string} elementId - ID of the error element
 * @param {string} message - Error message to display
 */
function showAuthError(elementId, message) {
  const errorEl = document.getElementById(elementId);
  if (errorEl) {
    errorEl.textContent = message;
    errorEl.hidden = false;
  }
}

/**
 * Hide error message on auth forms.
 * @param {string} elementId - ID of the error element
 */
function hideAuthError(elementId) {
  const errorEl = document.getElementById(elementId);
  if (errorEl) {
    errorEl.hidden = true;
  }
}

// Login form handler
const loginForm = document.getElementById('login-form');
if (loginForm) {
  loginForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideAuthError('login-error');

    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value;

    if (!email || !password) {
      showAuthError('login-error', 'メールアドレスとパスワードを入力してください');
      return;
    }

    try {
      const response = await apiFetch('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      });

      if (!response.ok) {
        const error = await response.json();
        showAuthError('login-error', error.error || 'ログインに失敗しました');
        return;
      }

      const data = await response.json();
      localStorage.setItem('authToken', data.data.accessToken);
      localStorage.setItem('refreshToken', data.data.refreshToken);
      window.location.href = '/';
    } catch (error) {
      console.error('Login failed:', error);
      showAuthError('login-error', 'ログインに失敗しました');
    }
  });
}

// Register form handler
const registerForm = document.getElementById('register-form');
if (registerForm) {
  registerForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideAuthError('register-error');

    const email = document.getElementById('email').value.trim();
    const password = document.getElementById('password').value;
    const confirmPassword = document.getElementById('confirm-password').value;

    if (!email || !password) {
      showAuthError('register-error', 'メールアドレスとパスワードを入力してください');
      return;
    }

    if (password !== confirmPassword) {
      showAuthError('register-error', 'パスワードが一致しません');
      return;
    }

    if (password.length < 8) {
      showAuthError('register-error', 'パスワードは8文字以上で入力してください');
      return;
    }

    try {
      const response = await apiFetch('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      });

      if (!response.ok) {
        const error = await response.json();
        showAuthError('register-error', error.error || '登録に失敗しました');
        return;
      }

      const data = await response.json();
      localStorage.setItem('authToken', data.data.accessToken);
      localStorage.setItem('refreshToken', data.data.refreshToken);
      window.location.href = '/';
    } catch (error) {
      console.error('Registration failed:', error);
      showAuthError('register-error', '登録に失敗しました');
    }
  });
}
