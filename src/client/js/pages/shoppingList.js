'use strict';

/**
 * Get the Monday of the week containing the given date.
 * (Shared utility — mirrors mealPlanBoard.js)
 * @param {Date} date
 * @returns {Date}
 */
function getShoppingMondayOf(date) {
  const d = new Date(date);
  const day = d.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  d.setDate(d.getDate() + diff);
  d.setHours(0, 0, 0, 0);
  return d;
}

/**
 * Format a Date to "yyyy-MM-dd".
 * @param {Date} date
 * @returns {string}
 */
function shoppingToIsoDate(date) {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/**
 * Render the shopping list page.
 * @param {HTMLElement} container
 */
async function renderShoppingListPage(container) {
  container.innerHTML = '';

  let currentMonday = getShoppingMondayOf(new Date());

  /**
   * Build the shopping list UI for the given week start.
   * @param {Date} monday
   */
  async function renderWeek(monday) {
    container.innerHTML = '';

    const weekStart = shoppingToIsoDate(monday);

    // ── Header ────────────────────────────────────────────────────────────
    const header = document.createElement('div');
    header.className = 'page-header';

    const title = document.createElement('h2');
    title.textContent = '買い物リスト';
    header.appendChild(title);

    container.appendChild(header);

    // ── Week navigation ───────────────────────────────────────────────────
    const nav = document.createElement('div');
    nav.className = 'meal-plan-nav';

    const prevBtn = document.createElement('button');
    prevBtn.className = 'btn btn-secondary';
    prevBtn.textContent = '← 前週';
    prevBtn.addEventListener('click', () => {
      currentMonday = new Date(monday);
      currentMonday.setDate(currentMonday.getDate() - 7);
      renderWeek(currentMonday);
    });

    const todayBtn = document.createElement('button');
    todayBtn.className = 'btn btn-secondary';
    todayBtn.textContent = '今週';
    todayBtn.addEventListener('click', () => {
      currentMonday = getShoppingMondayOf(new Date());
      renderWeek(currentMonday);
    });

    const nextBtn = document.createElement('button');
    nextBtn.className = 'btn btn-secondary';
    nextBtn.textContent = '翌週 →';
    nextBtn.addEventListener('click', () => {
      currentMonday = new Date(monday);
      currentMonday.setDate(currentMonday.getDate() + 7);
      renderWeek(currentMonday);
    });

    const endDate = new Date(monday);
    endDate.setDate(endDate.getDate() + 6);
    const weekLabel = document.createElement('span');
    weekLabel.className = 'week-label';
    weekLabel.textContent = `${monday.getMonth() + 1}/${monday.getDate()} 〜 ${endDate.getMonth() + 1}/${endDate.getDate()}`;

    nav.append(prevBtn, weekLabel, todayBtn, nextBtn);
    container.appendChild(nav);

    // ── Content ───────────────────────────────────────────────────────────
    const content = document.createElement('div');
    content.className = 'shopping-content';
    container.appendChild(content);

    const loadingMsg = document.createElement('p');
    loadingMsg.textContent = '読み込み中...';
    content.appendChild(loadingMsg);

    try {
      const data = await ShoppingApi.getList(weekStart);
      content.removeChild(loadingMsg);
      renderItems(content, weekStart, data.items);
    } catch (error) {
      content.removeChild(loadingMsg);
      const errMsg = document.createElement('p');
      errMsg.className = 'error-message';
      errMsg.textContent = '買い物リストの取得に失敗しました: ' + error.message;
      content.appendChild(errMsg);
    }
  }

  /**
   * Render the shopping item list into the given container.
   * @param {HTMLElement} container
   * @param {string} weekStart
   * @param {Array} items
   */
  function renderItems(container, weekStart, items) {
    if (!items || items.length === 0) {
      const emptyMsg = document.createElement('p');
      emptyMsg.className = 'empty-message';
      emptyMsg.textContent = 'この週の献立に材料が登録されていません。';
      container.appendChild(emptyMsg);
      return;
    }

    const list = document.createElement('ul');
    list.className = 'shopping-list';

    for (const item of items) {
      const li = createShoppingItemEl(item, weekStart);
      list.appendChild(li);
    }

    container.appendChild(list);
  }

  /**
   * Create a shopping list item element.
   * @param {{ingredientName: string, totalQuantity: string|null, unit: string|null, isChecked: boolean, sources: Array}} item
   * @param {string} weekStart
   * @returns {HTMLElement}
   */
  function createShoppingItemEl(item, weekStart) {
    const li = document.createElement('li');
    li.className = 'shopping-item' + (item.isChecked ? ' checked' : '');

    // Checkbox
    const checkbox = document.createElement('input');
    checkbox.type = 'checkbox';
    checkbox.className = 'shopping-checkbox';
    checkbox.checked = item.isChecked;
    checkbox.setAttribute('aria-label', item.ingredientName);

    checkbox.addEventListener('change', async () => {
      const newChecked = checkbox.checked;
      li.classList.toggle('checked', newChecked);
      checkbox.disabled = true;
      try {
        await ShoppingApi.updateCheck(weekStart, item.ingredientName, newChecked);
      } catch (error) {
        // Revert on failure
        checkbox.checked = !newChecked;
        li.classList.toggle('checked', !newChecked);
        console.error('チェック更新失敗:', error);
      } finally {
        checkbox.disabled = false;
      }
    });

    // Item label
    const label = document.createElement('span');
    label.className = 'shopping-item-label';

    const nameSpan = document.createElement('span');
    nameSpan.className = 'shopping-item-name';
    nameSpan.textContent = item.ingredientName;

    label.appendChild(nameSpan);

    // Quantity display
    if (item.totalQuantity !== null && item.totalQuantity !== undefined) {
      const qtySpan = document.createElement('span');
      qtySpan.className = 'shopping-item-qty';
      qtySpan.textContent = item.unit
        ? ` ${item.totalQuantity} ${item.unit}`
        : ` ${item.totalQuantity}`;
      label.appendChild(qtySpan);
    }

    // Sources (which recipes use this ingredient)
    if (item.sources && item.sources.length > 0) {
      const sources = document.createElement('span');
      sources.className = 'shopping-item-sources';
      const sourceNames = item.sources.map(s => {
        if (s.quantity && s.unit) return `${s.recipeName}(${s.quantity}${s.unit})`;
        if (s.quantity) return `${s.recipeName}(${s.quantity})`;
        return s.recipeName;
      });
      sources.textContent = `（${sourceNames.join('、')}）`;
      label.appendChild(sources);
    }

    li.append(checkbox, label);
    return li;
  }

  // Initial render
  renderWeek(currentMonday);
}
