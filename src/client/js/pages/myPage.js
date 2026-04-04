'use strict';

/**
 * My Page for MenuCraft.
 * Renders account info, group info, and password change form.
 *
 * @param {HTMLElement} container
 */
function renderMyPage(container) {
  container.innerHTML = '';

  const page = document.createElement('div');
  page.className = 'mypage';
  container.appendChild(page);

  const title = document.createElement('h1');
  title.textContent = 'マイページ';
  page.appendChild(title);

  // Placeholder while loading
  const loadingMsg = document.createElement('p');
  loadingMsg.textContent = '読み込み中...';
  page.appendChild(loadingMsg);

  ProfileApi.getProfile()
    .then((profile) => {
      page.removeChild(loadingMsg);
      _renderProfileSections(page, profile);
    })
    .catch(() => {
      loadingMsg.textContent = 'プロフィールの取得に失敗しました';
    });
}

/**
 * Render the profile sections once data is loaded.
 * @param {HTMLElement} page
 * @param {object} profile
 */
function _renderProfileSections(page, profile) {
  // ── Account info section ─────────────────────────────────────────────────
  const accountSection = _createSection('アカウント情報');
  _appendLabeledRow(accountSection, 'メールアドレス', profile.email);
  _appendLabeledRow(accountSection, 'ロール', profile.role);
  page.appendChild(accountSection);

  // ── Group info section ───────────────────────────────────────────────────
  if (profile.familyGroupId) {
    const groupSection = _createSection('グループ情報');
    _appendLabeledRow(groupSection, 'グループ名', profile.groupName || '—');

    const inviteRow = document.createElement('div');
    inviteRow.className = 'mypage-row';

    const inviteLabel = document.createElement('span');
    inviteLabel.className = 'mypage-label';
    inviteLabel.textContent = '招待コード';

    const inviteDisplay = document.createElement('div');
    inviteDisplay.className = 'invite-code-display';

    const inviteCode = document.createElement('code');
    inviteCode.textContent = profile.inviteCode || '—';

    const copyBtn = document.createElement('button');
    copyBtn.className = 'btn-sm';
    copyBtn.textContent = 'コピー';
    copyBtn.addEventListener('click', () => {
      navigator.clipboard.writeText(profile.inviteCode || '').then(() => {
        copyBtn.textContent = 'コピーしました';
        setTimeout(() => { copyBtn.textContent = 'コピー'; }, 2000);
      });
    });

    inviteDisplay.append(inviteCode, copyBtn);
    inviteRow.append(inviteLabel, inviteDisplay);
    groupSection.appendChild(inviteRow);

    const leaveBtn = document.createElement('button');
    leaveBtn.className = 'leave-group-btn btn-danger';
    leaveBtn.textContent = 'グループを脱退する';
    leaveBtn.addEventListener('click', async () => {
      if (!window.confirm('グループを脱退しますか？グループのデータにアクセスできなくなります。')) return;
      try {
        const tokens = await ProfileApi.leaveGroup();
        localStorage.setItem('authToken', tokens.accessToken);
        localStorage.setItem('refreshToken', tokens.refreshToken);
        window.location.reload();
      } catch {
        alert('グループ脱退に失敗しました');
      }
    });
    groupSection.appendChild(leaveBtn);

    page.appendChild(groupSection);
  }

  // ── Password change section ──────────────────────────────────────────────
  const pwSection = _createSection('パスワード変更');
  const form = _createPasswordForm();
  pwSection.appendChild(form);
  page.appendChild(pwSection);
}

/**
 * Create the password change form.
 * @returns {HTMLFormElement}
 */
function _createPasswordForm() {
  const form = document.createElement('form');
  form.className = 'password-change-form';

  form.appendChild(_createFormField('現在のパスワード', 'currentPassword', 'password'));
  form.appendChild(_createFormField('新しいパスワード', 'newPassword', 'password'));
  form.appendChild(_createFormField('新しいパスワード（確認）', 'confirmPassword', 'password'));

  const errorMsg = document.createElement('p');
  errorMsg.className = 'error-message';
  errorMsg.style.display = 'none';
  form.appendChild(errorMsg);

  const submitBtn = document.createElement('button');
  submitBtn.type = 'submit';
  submitBtn.textContent = 'パスワードを変更する';
  form.appendChild(submitBtn);

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    errorMsg.style.display = 'none';

    const currentPassword = form.querySelector('[name="currentPassword"]').value;
    const newPassword = form.querySelector('[name="newPassword"]').value;
    const confirmPassword = form.querySelector('[name="confirmPassword"]').value;

    if (newPassword !== confirmPassword) {
      errorMsg.textContent = '新しいパスワードが一致しません';
      errorMsg.style.display = 'block';
      return;
    }

    if (newPassword.length < 8) {
      errorMsg.textContent = 'パスワードは8文字以上で入力してください';
      errorMsg.style.display = 'block';
      return;
    }

    try {
      submitBtn.disabled = true;
      const tokens = await ProfileApi.changePassword(currentPassword, newPassword);
      localStorage.setItem('authToken', tokens.accessToken);
      localStorage.setItem('refreshToken', tokens.refreshToken);
      alert('パスワードを変更しました');
      form.reset();
    } catch {
      errorMsg.textContent = 'パスワード変更に失敗しました';
      errorMsg.style.display = 'block';
    } finally {
      submitBtn.disabled = false;
    }
  });

  return form;
}

// ── Private helpers ──────────────────────────────────────────────────────────

function _createSection(titleText) {
  const section = document.createElement('section');
  section.className = 'mypage-section';

  const h2 = document.createElement('h2');
  h2.textContent = titleText;
  section.appendChild(h2);

  return section;
}

function _appendLabeledRow(section, label, value) {
  const row = document.createElement('div');
  row.className = 'mypage-row';

  const labelEl = document.createElement('span');
  labelEl.className = 'mypage-label';
  labelEl.textContent = label;

  const valueEl = document.createElement('span');
  valueEl.className = 'mypage-value';
  valueEl.textContent = value;

  row.append(labelEl, valueEl);
  section.appendChild(row);
}

function _createFormField(labelText, name, type) {
  const wrapper = document.createElement('div');
  wrapper.className = 'form-group';

  const label = document.createElement('label');
  label.textContent = labelText;
  label.setAttribute('for', name);

  const input = document.createElement('input');
  input.type = type;
  input.name = name;
  input.id = name;
  input.required = true;

  wrapper.append(label, input);
  return wrapper;
}

// Export for testing (CommonJS); in browser renderMyPage is a global
if (typeof module !== 'undefined' && module.exports) {
  module.exports = { renderMyPage };
}
