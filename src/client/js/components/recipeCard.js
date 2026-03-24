'use strict';

/**
 * Create a recipe card DOM element.
 * @param {object} recipe - Recipe data
 * @param {object} [callbacks] - Event callbacks
 * @param {function} [callbacks.onSelect] - Called when card is clicked
 * @param {function} [callbacks.onDelete] - Called when delete button is clicked
 * @returns {HTMLElement}
 */
function createRecipeCard(recipe, { onSelect, onDelete } = {}) {
  const card = document.createElement('div');
  card.className = 'recipe-card';
  card.dataset.recipeId = recipe.id;

  if (recipe.imageUrl) {
    const img = document.createElement('img');
    img.src = recipe.imageUrl;
    img.alt = recipe.title;
    img.className = 'recipe-card-image';
    img.loading = 'lazy';
    card.appendChild(img);
  }

  const content = document.createElement('div');
  content.className = 'recipe-card-content';

  const title = document.createElement('h3');
  title.className = 'recipe-card-title';
  title.textContent = recipe.title;
  content.appendChild(title);

  if (recipe.description) {
    const desc = document.createElement('p');
    desc.className = 'recipe-card-description';
    desc.textContent = recipe.description.length > 80
      ? recipe.description.slice(0, 80) + '...'
      : recipe.description;
    content.appendChild(desc);
  }

  if (recipe.tags && recipe.tags.length > 0) {
    const tagsContainer = document.createElement('div');
    tagsContainer.className = 'recipe-card-tags';
    for (const tag of recipe.tags) {
      const tagEl = document.createElement('span');
      tagEl.className = 'tag';
      tagEl.textContent = tag;
      tagsContainer.appendChild(tagEl);
    }
    content.appendChild(tagsContainer);
  }

  const actions = document.createElement('div');
  actions.className = 'recipe-card-actions';

  if (onDelete) {
    const deleteBtn = document.createElement('button');
    deleteBtn.className = 'btn btn-danger btn-sm';
    deleteBtn.textContent = '削除';
    deleteBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      onDelete(recipe.id);
    });
    actions.appendChild(deleteBtn);
  }

  content.appendChild(actions);
  card.appendChild(content);

  if (onSelect) {
    card.style.cursor = 'pointer';
    card.addEventListener('click', () => onSelect(recipe));
  }

  return card;
}
