'use strict';

/**
 * Get the Monday of the week containing the given date.
 * @param {Date} date
 * @returns {Date}
 */
function getMondayOf(date) {
  const d = new Date(date);
  const day = d.getDay(); // 0=Sun, 1=Mon, ..., 6=Sat
  const diff = day === 0 ? -6 : 1 - day; // shift to Monday
  d.setDate(d.getDate() + diff);
  d.setHours(0, 0, 0, 0);
  return d;
}

/**
 * Format a Date to "yyyy-MM-dd".
 * @param {Date} date
 * @returns {string}
 */
function toIsoDate(date) {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/**
 * Format a date string "yyyy-MM-dd" to a human-readable label (e.g. "1/15 (月)").
 * @param {string} dateStr
 * @returns {string}
 */
function formatDateLabel(dateStr) {
  const d = new Date(dateStr + 'T00:00:00');
  const weekdays = ['日', '月', '火', '水', '木', '金', '土'];
  return `${d.getMonth() + 1}/${d.getDate()} (${weekdays[d.getDay()]})`;
}

/**
 * Render the meal plan board page.
 * @param {HTMLElement} container
 */
async function renderMealPlanBoard(container) {
  container.innerHTML = '';

  let currentMonday = getMondayOf(new Date());

  /**
   * Build the full board UI for the given week start.
   * @param {Date} monday
   */
  async function renderWeek(monday) {
    container.innerHTML = '';

    const weekStart = toIsoDate(monday);

    // ── Header ────────────────────────────────────────────────────────────
    const header = document.createElement('div');
    header.className = 'page-header';

    const title = document.createElement('h2');
    title.textContent = '献立ボード';
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
      currentMonday = getMondayOf(new Date());
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

    const weekLabel = document.createElement('span');
    weekLabel.className = 'week-label';
    const endDate = new Date(monday);
    endDate.setDate(endDate.getDate() + 6);
    weekLabel.textContent = `${monday.getMonth() + 1}/${monday.getDate()} 〜 ${endDate.getMonth() + 1}/${endDate.getDate()}`;

    nav.append(prevBtn, weekLabel, todayBtn, nextBtn);
    container.appendChild(nav);

    // ── Loading state ──────────────────────────────────────────────────────
    const loadingEl = document.createElement('p');
    loadingEl.className = 'loading-text';
    loadingEl.textContent = '読み込み中...';
    container.appendChild(loadingEl);

    // ── Fetch data ────────────────────────────────────────────────────────
    let weekData;
    let allRecipes;
    try {
      [weekData, allRecipes] = await Promise.all([
        MealPlansApi.getWeekly(weekStart),
        RecipesApi.getAll(),
      ]);
    } catch (error) {
      console.error('Failed to load meal plan data:', error);
      loadingEl.remove();
      const errorEl = document.createElement('p');
      errorEl.className = 'error-message';
      errorEl.textContent = '献立データの読み込みに失敗しました';
      container.appendChild(errorEl);
      return;
    }

    loadingEl.remove();

    // ── Board grid ────────────────────────────────────────────────────────
    const board = document.createElement('div');
    board.className = 'meal-plan-board';

    // Group plans by date
    const plansByDate = {};
    for (const plan of weekData.plans) {
      if (!plansByDate[plan.date]) {
        plansByDate[plan.date] = {};
      }
      plansByDate[plan.date][plan.mealType] = plan;
    }

    for (let i = 0; i < 7; i++) {
      const d = new Date(monday);
      d.setDate(d.getDate() + i);
      const dateStr = toIsoDate(d);

      const dayCol = document.createElement('div');
      dayCol.className = 'meal-plan-day';

      const dayHeader = document.createElement('div');
      dayHeader.className = 'meal-plan-day-header';
      dayHeader.textContent = formatDateLabel(dateStr);
      dayCol.appendChild(dayHeader);

      for (const mealType of ['Lunch', 'Dinner']) {
        const mealLabel = mealType === 'Lunch' ? '昼食' : '夕食';
        const plan = plansByDate[dateStr]?.[mealType];

        const cell = createMealCell(dateStr, mealType, mealLabel, plan, allRecipes, weekData, renderWeek, monday);
        dayCol.appendChild(cell);
      }

      board.appendChild(dayCol);
    }

    container.appendChild(board);
  }

  await renderWeek(currentMonday);
}

/**
 * Create a meal cell for a given date/mealType.
 * @param {string} dateStr
 * @param {string} mealType
 * @param {string} mealLabel
 * @param {object|undefined} plan
 * @param {Array} allRecipes
 * @param {object} weekData
 * @param {function} renderWeek
 * @param {Date} monday
 * @returns {HTMLElement}
 */
function createMealCell(dateStr, mealType, mealLabel, plan, allRecipes, weekData, renderWeek, monday) {
  const cell = document.createElement('div');
  cell.className = 'meal-cell';

  const cellHeader = document.createElement('div');
  cellHeader.className = 'meal-cell-header';

  const typeLabel = document.createElement('span');
  typeLabel.className = 'meal-type-label';
  typeLabel.textContent = mealLabel;
  cellHeader.appendChild(typeLabel);

  const addBtn = document.createElement('button');
  addBtn.className = 'btn btn-sm btn-secondary';
  addBtn.textContent = '+ 追加';
  addBtn.addEventListener('click', () => {
    const currentRecipeIds = (plan?.recipes ?? []).map((r) => r.id);
    openRecipeSelectModal(allRecipes, currentRecipeIds, async (selectedIds) => {
      try {
        await MealPlansApi.update(dateStr, mealType, selectedIds);
        await renderWeek(monday);
      } catch (error) {
        console.error('Failed to update meal plan:', error);
        alert('献立の更新に失敗しました');
      }
    });
  });
  cellHeader.appendChild(addBtn);

  cell.appendChild(cellHeader);

  const recipeList = document.createElement('ul');
  recipeList.className = 'meal-cell-recipes';

  const recipes = plan?.recipes ?? [];
  if (recipes.length === 0) {
    const empty = document.createElement('li');
    empty.className = 'meal-cell-empty';
    empty.textContent = '未設定';
    recipeList.appendChild(empty);
  } else {
    for (const recipe of recipes) {
      const item = createRecipeItem(recipe, async () => {
        // Remove this recipe from the cell
        const remaining = recipes.filter((r) => r.id !== recipe.id).map((r) => r.id);
        try {
          await MealPlansApi.update(dateStr, mealType, remaining);
          await renderWeek(monday);
        } catch (error) {
          console.error('Failed to remove recipe from meal plan:', error);
          alert('レシピの解除に失敗しました');
        }
      });
      recipeList.appendChild(item);
    }
  }

  cell.appendChild(recipeList);
  return cell;
}

/**
 * Create a recipe list item with a remove button.
 * @param {object} recipe
 * @param {function} onRemove
 * @returns {HTMLElement}
 */
function createRecipeItem(recipe, onRemove) {
  const item = document.createElement('li');
  item.className = 'meal-cell-recipe-item';

  if (recipe.imageUrl) {
    const img = document.createElement('img');
    img.src = recipe.imageUrl;
    img.alt = recipe.title;
    img.className = 'meal-cell-recipe-thumb';
    img.loading = 'lazy';
    item.appendChild(img);
  }

  const titleSpan = document.createElement('span');
  titleSpan.className = 'meal-cell-recipe-title';
  titleSpan.textContent = recipe.title;
  item.appendChild(titleSpan);

  const removeBtn = document.createElement('button');
  removeBtn.className = 'btn btn-sm btn-danger';
  removeBtn.textContent = '×';
  removeBtn.title = 'レシピを外す';
  removeBtn.addEventListener('click', (e) => {
    e.stopPropagation();
    onRemove();
  });
  item.appendChild(removeBtn);

  return item;
}

/**
 * Open a modal to select recipes from the full recipe list.
 * @param {Array} allRecipes - All available recipes
 * @param {number[]} currentIds - IDs already selected
 * @param {function} onConfirm - Called with the new list of selected IDs
 */
function openRecipeSelectModal(allRecipes, currentIds, onConfirm) {
  // Remove existing modal if any
  const existing = document.getElementById('recipe-select-modal');
  if (existing) {
    existing.remove();
  }

  const overlay = document.createElement('div');
  overlay.id = 'recipe-select-modal';
  overlay.className = 'modal-overlay';

  const modal = document.createElement('div');
  modal.className = 'modal';

  const modalHeader = document.createElement('div');
  modalHeader.className = 'modal-header';

  const modalTitle = document.createElement('h3');
  modalTitle.textContent = 'レシピを選択';
  modalHeader.appendChild(modalTitle);

  const closeBtn = document.createElement('button');
  closeBtn.className = 'modal-close';
  closeBtn.textContent = '×';
  closeBtn.addEventListener('click', () => overlay.remove());
  modalHeader.appendChild(closeBtn);

  modal.appendChild(modalHeader);

  const modalBody = document.createElement('div');
  modalBody.className = 'modal-body';

  const selectedIds = new Set(currentIds);

  if (allRecipes.length === 0) {
    const empty = document.createElement('p');
    empty.textContent = 'レシピがありません。先にレシピを登録してください。';
    modalBody.appendChild(empty);
  } else {
    const list = document.createElement('ul');
    list.className = 'recipe-select-list';

    for (const recipe of allRecipes) {
      const item = document.createElement('li');
      item.className = 'recipe-select-item';

      const label = document.createElement('label');
      label.className = 'recipe-select-label';

      const checkbox = document.createElement('input');
      checkbox.type = 'checkbox';
      checkbox.value = String(recipe.id);
      checkbox.checked = selectedIds.has(recipe.id);
      checkbox.addEventListener('change', () => {
        if (checkbox.checked) {
          selectedIds.add(recipe.id);
        } else {
          selectedIds.delete(recipe.id);
        }
      });

      const titleSpan = document.createElement('span');
      titleSpan.textContent = recipe.title;

      label.append(checkbox, titleSpan);
      item.appendChild(label);
      list.appendChild(item);
    }

    modalBody.appendChild(list);
  }

  modal.appendChild(modalBody);

  const modalFooter = document.createElement('div');
  modalFooter.className = 'modal-footer';

  const cancelBtn = document.createElement('button');
  cancelBtn.className = 'btn btn-secondary';
  cancelBtn.textContent = 'キャンセル';
  cancelBtn.addEventListener('click', () => overlay.remove());

  const confirmBtn = document.createElement('button');
  confirmBtn.className = 'btn btn-primary';
  confirmBtn.textContent = '確定';
  confirmBtn.addEventListener('click', () => {
    overlay.remove();
    onConfirm(Array.from(selectedIds));
  });

  modalFooter.append(cancelBtn, confirmBtn);
  modal.appendChild(modalFooter);

  overlay.appendChild(modal);

  // Close on backdrop click
  overlay.addEventListener('click', (e) => {
    if (e.target === overlay) {
      overlay.remove();
    }
  });

  document.body.appendChild(overlay);
}
