'use strict';

/**
 * Group setup page logic.
 * Handles creating a new family group or joining via invite code.
 */

/**
 * Render the group setup page into the main container.
 * @param {HTMLElement} container - The main content container
 */
function renderGroupSetup(container) {
  container.innerHTML = '';

  const wrapper = document.createElement('div');
  wrapper.className = 'auth-page';

  const card = document.createElement('div');
  card.className = 'auth-card';

  const title = document.createElement('h1');
  title.textContent = 'MenuCraft';
  card.appendChild(title);

  const subtitle = document.createElement('h2');
  subtitle.textContent = 'グループ設定';
  card.appendChild(subtitle);

  const description = document.createElement('p');
  description.className = 'auth-description';
  description.textContent = 'ログインしました。利用を開始するにはグループを作成するか、招待コードで既存のグループに参加してください。';
  card.appendChild(description);

  // Create group section
  const createSection = document.createElement('div');
  createSection.className = 'form-section';

  const createTitle = document.createElement('h3');
  createTitle.textContent = '新しいグループを作成';
  createSection.appendChild(createTitle);

  const createForm = document.createElement('form');
  createForm.id = 'create-group-form';

  const nameGroup = document.createElement('div');
  nameGroup.className = 'form-group';
  const nameLabel = document.createElement('label');
  nameLabel.setAttribute('for', 'group-name');
  nameLabel.textContent = 'グループ名';
  const nameInput = document.createElement('input');
  nameInput.type = 'text';
  nameInput.id = 'group-name';
  nameInput.name = 'name';
  nameInput.required = true;
  nameInput.maxLength = 100;
  nameGroup.appendChild(nameLabel);
  nameGroup.appendChild(nameInput);
  createForm.appendChild(nameGroup);

  const createBtn = document.createElement('button');
  createBtn.type = 'submit';
  createBtn.className = 'btn btn-primary';
  createBtn.textContent = 'グループを作成';
  createForm.appendChild(createBtn);

  createSection.appendChild(createForm);
  card.appendChild(createSection);

  // Divider
  const divider = document.createElement('hr');
  divider.style.margin = '1.5rem 0';
  card.appendChild(divider);

  // Join group section
  const joinSection = document.createElement('div');
  joinSection.className = 'form-section';

  const joinTitle = document.createElement('h3');
  joinTitle.textContent = '招待コードで参加';
  joinSection.appendChild(joinTitle);

  const joinForm = document.createElement('form');
  joinForm.id = 'join-group-form';

  const codeGroup = document.createElement('div');
  codeGroup.className = 'form-group';
  const codeLabel = document.createElement('label');
  codeLabel.setAttribute('for', 'invite-code');
  codeLabel.textContent = '招待コード';
  const codeInput = document.createElement('input');
  codeInput.type = 'text';
  codeInput.id = 'invite-code';
  codeInput.name = 'inviteCode';
  codeInput.required = true;
  codeInput.maxLength = 20;
  codeGroup.appendChild(codeLabel);
  codeGroup.appendChild(codeInput);
  joinForm.appendChild(codeGroup);

  const joinBtn = document.createElement('button');
  joinBtn.type = 'submit';
  joinBtn.className = 'btn btn-primary';
  joinBtn.textContent = '参加する';
  joinForm.appendChild(joinBtn);

  joinSection.appendChild(joinForm);
  card.appendChild(joinSection);

  // Error display
  const errorDiv = document.createElement('div');
  errorDiv.id = 'group-error';
  errorDiv.className = 'error-message';
  errorDiv.hidden = true;
  card.appendChild(errorDiv);

  const logoutLink = document.createElement('p');
  logoutLink.className = 'auth-link';
  const logoutBtn = document.createElement('button');
  logoutBtn.className = 'btn-link';
  logoutBtn.textContent = 'ログアウト';
  logoutBtn.addEventListener('click', () => {
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    window.location.href = '/login.html';
  });
  logoutLink.appendChild(logoutBtn);
  card.appendChild(logoutLink);

  wrapper.appendChild(card);
  container.appendChild(wrapper);

  // Event listeners
  createForm.addEventListener('submit', handleCreateGroup);
  joinForm.addEventListener('submit', handleJoinGroup);
}

async function handleCreateGroup(e) {
  e.preventDefault();
  const errorDiv = document.getElementById('group-error');
  if (errorDiv) errorDiv.hidden = true;

  const name = document.getElementById('group-name').value.trim();
  if (!name) {
    showGroupError('グループ名を入力してください');
    return;
  }

  try {
    const data = await GroupsApi.create(name);
    if (!data || !data.success) {
      showGroupError((data && data.error) || 'グループの作成に失敗しました');
      return;
    }

    // Re-login to obtain a fresh token that includes familyGroupId
    window.location.href = '/';
  } catch (error) {
    console.error('Failed to create group:', error);
    showGroupError('グループの作成に失敗しました');
  }
}

async function handleJoinGroup(e) {
  e.preventDefault();
  const errorDiv = document.getElementById('group-error');
  if (errorDiv) errorDiv.hidden = true;

  const inviteCode = document.getElementById('invite-code').value.trim();
  if (!inviteCode) {
    showGroupError('招待コードを入力してください');
    return;
  }

  try {
    const data = await GroupsApi.join(inviteCode);
    if (!data || !data.success) {
      showGroupError((data && data.error) || '参加に失敗しました');
      return;
    }

    // Re-login to obtain a fresh token that includes familyGroupId
    window.location.href = '/login.html?hint=group_joined';
  } catch (error) {
    console.error('Failed to join group:', error);
    showGroupError('参加に失敗しました');
  }
}

function showGroupError(message) {
  const errorDiv = document.getElementById('group-error');
  if (errorDiv) {
    errorDiv.textContent = message;
    errorDiv.hidden = false;
  }
}
