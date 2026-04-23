'use strict';
/**
 * Tests for auth.js - login/register form handlers.
 * Covers the spinner + disable-input "loading" UX during submission.
 * @jest-environment jsdom
 */

let auth;

function setupLoginDom() {
  document.body.innerHTML = `
    <form id="login-form">
      <input type="email" id="email" />
      <input type="password" id="password" />
      <div id="login-error" class="error-message" hidden></div>
      <button type="submit" id="login-submit" class="btn btn-primary">
        <span class="btn-label">ログイン</span>
        <span class="spinner" hidden aria-hidden="true"></span>
      </button>
    </form>
  `;
}

function setupRegisterDom() {
  document.body.innerHTML = `
    <form id="register-form">
      <input type="email" id="email" />
      <input type="password" id="password" />
      <input type="password" id="confirm-password" />
      <div id="register-error" class="error-message" hidden></div>
      <button type="submit" id="register-submit" class="btn btn-primary">
        <span class="btn-label">登録</span>
        <span class="spinner" hidden aria-hidden="true"></span>
      </button>
    </form>
  `;
}

function loadAuth() {
  const mod = require('../../api/auth');
  auth = mod;
  // Override redirect so jsdom doesn't actually navigate
  auth._setRedirect(jest.fn());
  return auth;
}

beforeEach(() => {
  jest.resetModules();

  const storage = {};
  jest.spyOn(Storage.prototype, 'getItem').mockImplementation((key) => storage[key] ?? null);
  jest.spyOn(Storage.prototype, 'setItem').mockImplementation((key, val) => { storage[key] = val; });
  jest.spyOn(Storage.prototype, 'removeItem').mockImplementation((key) => { delete storage[key]; });

  global.AppConfig = { API_BASE_URL: '' };
  global.apiFetch = jest.fn();

  jest.spyOn(console, 'error').mockImplementation(() => {});
});

afterEach(() => {
  jest.restoreAllMocks();
  delete global.AppConfig;
  delete global.apiFetch;
  document.body.innerHTML = '';
});

function fillLogin(email, password) {
  document.getElementById('email').value = email;
  document.getElementById('password').value = password;
}

function fillRegister(email, password, confirmPassword) {
  document.getElementById('email').value = email;
  document.getElementById('password').value = password;
  document.getElementById('confirm-password').value = confirmPassword;
}

function submitForm(formId) {
  const form = document.getElementById(formId);
  const event = new Event('submit', { cancelable: true, bubbles: true });
  form.dispatchEvent(event);
  return event;
}

function makeOkResponse(data) {
  return {
    ok: true,
    json: jest.fn().mockResolvedValue(data),
  };
}

function makeErrResponse(errorBody) {
  return {
    ok: false,
    json: jest.fn().mockResolvedValue(errorBody),
  };
}

describe('login form - loading state', () => {
  beforeEach(() => {
    setupLoginDom();
    loadAuth();
  });

  test('送信中: 入力・ボタンが disabled になり、スピナーが表示される', async () => {
    let resolveFetch;
    global.apiFetch.mockReturnValue(new Promise((resolve) => { resolveFetch = resolve; }));

    fillLogin('user@example.com', 'password123');
    submitForm('login-form');

    // microtask flush so handler enters awaited state
    await Promise.resolve();

    expect(document.getElementById('email').disabled).toBe(true);
    expect(document.getElementById('password').disabled).toBe(true);
    expect(document.getElementById('login-submit').disabled).toBe(true);
    expect(document.getElementById('login-submit').getAttribute('aria-busy')).toBe('true');
    expect(document.querySelector('#login-submit .spinner').hidden).toBe(false);
    expect(document.querySelector('#login-submit .btn-label').textContent).toBe('ログイン中…');

    resolveFetch(makeOkResponse({ data: { accessToken: 'a', refreshToken: 'r' } }));
    await new Promise((resolve) => setTimeout(resolve, 0));
  });

  test('成功後: ロックが解除され、画面遷移が呼ばれる', async () => {
    const redirect = jest.fn();
    auth._setRedirect(redirect);

    global.apiFetch.mockResolvedValue(
      makeOkResponse({ data: { accessToken: 'access-1', refreshToken: 'refresh-1' } }),
    );

    fillLogin('user@example.com', 'password123');
    submitForm('login-form');
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(redirect).toHaveBeenCalledWith('/');
    expect(localStorage.setItem).toHaveBeenCalledWith('authToken', 'access-1');
    expect(localStorage.setItem).toHaveBeenCalledWith('refreshToken', 'refresh-1');

    expect(document.getElementById('email').disabled).toBe(false);
    expect(document.getElementById('password').disabled).toBe(false);
    expect(document.getElementById('login-submit').disabled).toBe(false);
    expect(document.querySelector('#login-submit .spinner').hidden).toBe(true);
    expect(document.querySelector('#login-submit .btn-label').textContent).toBe('ログイン');
  });

  test('失敗レスポンス: ロックが解除され、エラーメッセージを表示する', async () => {
    global.apiFetch.mockResolvedValue(makeErrResponse({ error: '資格情報が不正です' }));

    fillLogin('user@example.com', 'wrong-password');
    submitForm('login-form');
    await new Promise((resolve) => setTimeout(resolve, 0));

    const errorEl = document.getElementById('login-error');
    expect(errorEl.hidden).toBe(false);
    expect(errorEl.textContent).toBe('資格情報が不正です');

    expect(document.getElementById('email').disabled).toBe(false);
    expect(document.getElementById('password').disabled).toBe(false);
    expect(document.getElementById('login-submit').disabled).toBe(false);
    expect(document.querySelector('#login-submit .spinner').hidden).toBe(true);
    expect(document.querySelector('#login-submit .btn-label').textContent).toBe('ログイン');
  });

  test('ネットワーク例外: ロックが解除され、汎用エラーを表示する', async () => {
    global.apiFetch.mockRejectedValue(new Error('network down'));

    fillLogin('user@example.com', 'password123');
    submitForm('login-form');
    await new Promise((resolve) => setTimeout(resolve, 0));

    const errorEl = document.getElementById('login-error');
    expect(errorEl.hidden).toBe(false);
    expect(errorEl.textContent).toBe('ログインに失敗しました');

    expect(document.getElementById('login-submit').disabled).toBe(false);
    expect(document.querySelector('#login-submit .spinner').hidden).toBe(true);
  });

  test('連打防止: 送信中にもう一度 submit しても API は1度しか呼ばれない', async () => {
    let resolveFetch;
    global.apiFetch.mockReturnValue(new Promise((resolve) => { resolveFetch = resolve; }));

    fillLogin('user@example.com', 'password123');
    submitForm('login-form');
    await Promise.resolve();
    submitForm('login-form');
    submitForm('login-form');

    expect(global.apiFetch).toHaveBeenCalledTimes(1);

    resolveFetch(makeOkResponse({ data: { accessToken: 'a', refreshToken: 'r' } }));
    await new Promise((resolve) => setTimeout(resolve, 0));
  });

  test('入力未入力時は API を呼ばずバリデーションエラーを表示する', async () => {
    fillLogin('', '');
    submitForm('login-form');
    await Promise.resolve();

    expect(global.apiFetch).not.toHaveBeenCalled();
    const errorEl = document.getElementById('login-error');
    expect(errorEl.hidden).toBe(false);
    expect(errorEl.textContent).toContain('メールアドレス');
  });
});

describe('register form - loading state', () => {
  beforeEach(() => {
    setupRegisterDom();
    loadAuth();
  });

  test('送信中: 3つの入力とボタンが disabled になり、スピナーが表示される', async () => {
    let resolveFetch;
    global.apiFetch.mockReturnValue(new Promise((resolve) => { resolveFetch = resolve; }));

    fillRegister('user@example.com', 'password123', 'password123');
    submitForm('register-form');
    await Promise.resolve();

    expect(document.getElementById('email').disabled).toBe(true);
    expect(document.getElementById('password').disabled).toBe(true);
    expect(document.getElementById('confirm-password').disabled).toBe(true);
    expect(document.getElementById('register-submit').disabled).toBe(true);
    expect(document.getElementById('register-submit').getAttribute('aria-busy')).toBe('true');
    expect(document.querySelector('#register-submit .spinner').hidden).toBe(false);
    expect(document.querySelector('#register-submit .btn-label').textContent).toBe('登録中…');

    resolveFetch(makeOkResponse({ data: { accessToken: 'a', refreshToken: 'r' } }));
    await new Promise((resolve) => setTimeout(resolve, 0));
  });

  test('成功後: ロックが解除され、画面遷移が呼ばれる', async () => {
    const redirect = jest.fn();
    auth._setRedirect(redirect);

    global.apiFetch.mockResolvedValue(
      makeOkResponse({ data: { accessToken: 'acc', refreshToken: 'ref' } }),
    );

    fillRegister('user@example.com', 'password123', 'password123');
    submitForm('register-form');
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(redirect).toHaveBeenCalledWith('/');
    expect(document.getElementById('email').disabled).toBe(false);
    expect(document.getElementById('password').disabled).toBe(false);
    expect(document.getElementById('confirm-password').disabled).toBe(false);
    expect(document.getElementById('register-submit').disabled).toBe(false);
    expect(document.querySelector('#register-submit .btn-label').textContent).toBe('登録');
  });

  test('失敗レスポンス: ロックが解除され、エラーメッセージを表示する', async () => {
    global.apiFetch.mockResolvedValue(makeErrResponse({ error: 'メール重複' }));

    fillRegister('user@example.com', 'password123', 'password123');
    submitForm('register-form');
    await new Promise((resolve) => setTimeout(resolve, 0));

    const errorEl = document.getElementById('register-error');
    expect(errorEl.hidden).toBe(false);
    expect(errorEl.textContent).toBe('メール重複');
    expect(document.getElementById('register-submit').disabled).toBe(false);
    expect(document.querySelector('#register-submit .spinner').hidden).toBe(true);
  });

  test('連打防止: 送信中にもう一度 submit しても API は1度しか呼ばれない', async () => {
    let resolveFetch;
    global.apiFetch.mockReturnValue(new Promise((resolve) => { resolveFetch = resolve; }));

    fillRegister('user@example.com', 'password123', 'password123');
    submitForm('register-form');
    await Promise.resolve();
    submitForm('register-form');
    submitForm('register-form');

    expect(global.apiFetch).toHaveBeenCalledTimes(1);

    resolveFetch(makeOkResponse({ data: { accessToken: 'a', refreshToken: 'r' } }));
    await new Promise((resolve) => setTimeout(resolve, 0));
  });

  test('パスワード不一致の場合は API を呼ばずバリデーションエラーを表示する', async () => {
    fillRegister('user@example.com', 'password123', 'different');
    submitForm('register-form');
    await Promise.resolve();

    expect(global.apiFetch).not.toHaveBeenCalled();
    const errorEl = document.getElementById('register-error');
    expect(errorEl.hidden).toBe(false);
    expect(errorEl.textContent).toContain('一致');
  });
});
