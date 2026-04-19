'use strict';

/**
 * Auth API client and form handlers.
 */

let _redirect = (url) => {
  window.location.href = url;
};

let _isLoginSubmitting = false;
let _isRegisterSubmitting = false;

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

/**
 * Toggle loading state for an auth form: disables/enables inputs and button,
 * shows/hides the spinner, swaps the button label.
 * @param {{submitBtnId: string, inputIds: string[], loadingLabel: string, idleLabel: string}} config
 * @param {boolean} loading
 */
function setFormLoading(config, loading) {
  const btn = document.getElementById(config.submitBtnId);
  if (btn) {
    btn.disabled = loading;
    btn.setAttribute('aria-busy', loading ? 'true' : 'false');
    const label = btn.querySelector('.btn-label');
    if (label) {
      label.textContent = loading ? config.loadingLabel : config.idleLabel;
    }
    const spinner = btn.querySelector('.spinner');
    if (spinner) {
      spinner.hidden = !loading;
    }
  }
  config.inputIds.forEach((id) => {
    const el = document.getElementById(id);
    if (el) el.disabled = loading;
  });
}

const LOGIN_LOADING_CONFIG = {
  submitBtnId: 'login-submit',
  inputIds: ['email', 'password'],
  loadingLabel: 'ログイン中…',
  idleLabel: 'ログイン',
};

const REGISTER_LOADING_CONFIG = {
  submitBtnId: 'register-submit',
  inputIds: ['email', 'password', 'confirm-password'],
  loadingLabel: '登録中…',
  idleLabel: '登録',
};

async function handleLoginSubmit(e) {
  e.preventDefault();
  if (_isLoginSubmitting) return;
  hideAuthError('login-error');

  const email = document.getElementById('email').value.trim();
  const password = document.getElementById('password').value;

  if (!email || !password) {
    showAuthError('login-error', 'メールアドレスとパスワードを入力してください');
    return;
  }

  _isLoginSubmitting = true;
  setFormLoading(LOGIN_LOADING_CONFIG, true);
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
    _redirect('/');
  } catch (error) {
    console.error('Login failed:', error);
    showAuthError('login-error', 'ログインに失敗しました');
  } finally {
    _isLoginSubmitting = false;
    setFormLoading(LOGIN_LOADING_CONFIG, false);
  }
}

async function handleRegisterSubmit(e) {
  e.preventDefault();
  if (_isRegisterSubmitting) return;
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

  _isRegisterSubmitting = true;
  setFormLoading(REGISTER_LOADING_CONFIG, true);
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
    _redirect('/');
  } catch (error) {
    console.error('Registration failed:', error);
    showAuthError('register-error', '登録に失敗しました');
  } finally {
    _isRegisterSubmitting = false;
    setFormLoading(REGISTER_LOADING_CONFIG, false);
  }
}

const loginForm = document.getElementById('login-form');
if (loginForm) {
  loginForm.addEventListener('submit', handleLoginSubmit);
}

const registerForm = document.getElementById('register-form');
if (registerForm) {
  registerForm.addEventListener('submit', handleRegisterSubmit);
}

if (typeof module !== 'undefined' && module.exports) {
  module.exports = {
    handleLoginSubmit,
    handleRegisterSubmit,
    setFormLoading,
    _setRedirect: (fn) => { _redirect = fn; },
  };
}
