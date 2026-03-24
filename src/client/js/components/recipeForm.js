'use strict';

/**
 * Create a recipe form (add/edit) DOM element.
 * @param {object} [recipe] - Existing recipe data for editing (null for new)
 * @param {object} callbacks
 * @param {function} callbacks.onSubmit - Called with form data on submit
 * @param {function} callbacks.onCancel - Called when cancel is clicked
 * @returns {HTMLElement}
 */
function createRecipeForm(recipe, { onSubmit, onCancel }) {
  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay';

  const modal = document.createElement('div');
  modal.className = 'modal';

  const heading = document.createElement('h2');
  heading.textContent = recipe ? 'レシピを編集' : 'レシピを追加';
  modal.appendChild(heading);

  const form = document.createElement('form');
  form.id = 'recipe-form';

  // Title
  form.appendChild(createFormField('title', 'タイトル', 'text', recipe?.title || '', true));

  // URL
  form.appendChild(createFormField('url', 'URL', 'url', recipe?.url || '', false));

  // Image URL
  form.appendChild(createFormField('imageUrl', '画像URL', 'url', recipe?.imageUrl || '', false));

  // Description
  const descGroup = document.createElement('div');
  descGroup.className = 'form-group';
  const descLabel = document.createElement('label');
  descLabel.setAttribute('for', 'description');
  descLabel.textContent = '説明';
  const descTextarea = document.createElement('textarea');
  descTextarea.id = 'description';
  descTextarea.name = 'description';
  descTextarea.rows = 3;
  descTextarea.maxLength = 4000;
  descTextarea.value = recipe?.description || '';
  descGroup.appendChild(descLabel);
  descGroup.appendChild(descTextarea);
  form.appendChild(descGroup);

  // Tags
  form.appendChild(createFormField('tags', 'タグ（カンマ区切り）', 'text',
    recipe?.tags?.join(', ') || '', false));

  // Ingredients (simple text area for MVP)
  const ingredGroup = document.createElement('div');
  ingredGroup.className = 'form-group';
  const ingredLabel = document.createElement('label');
  ingredLabel.setAttribute('for', 'ingredients');
  ingredLabel.textContent = '材料（1行に1つ: 名前 数量 単位）';
  const ingredTextarea = document.createElement('textarea');
  ingredTextarea.id = 'ingredients';
  ingredTextarea.name = 'ingredients';
  ingredTextarea.rows = 5;
  ingredTextarea.value = recipe?.ingredients
    ?.map(i => `${i.name} ${i.quantity || ''} ${i.unit || ''}`.trim())
    .join('\n') || '';
  ingredGroup.appendChild(ingredLabel);
  ingredGroup.appendChild(ingredTextarea);
  form.appendChild(ingredGroup);

  // Error
  const errorDiv = document.createElement('div');
  errorDiv.id = 'recipe-form-error';
  errorDiv.className = 'error-message';
  errorDiv.hidden = true;
  form.appendChild(errorDiv);

  // Buttons
  const btnGroup = document.createElement('div');
  btnGroup.className = 'form-actions';

  const submitBtn = document.createElement('button');
  submitBtn.type = 'submit';
  submitBtn.className = 'btn btn-primary';
  submitBtn.textContent = recipe ? '更新' : '追加';
  btnGroup.appendChild(submitBtn);

  const cancelBtn = document.createElement('button');
  cancelBtn.type = 'button';
  cancelBtn.className = 'btn';
  cancelBtn.textContent = 'キャンセル';
  cancelBtn.addEventListener('click', () => {
    overlay.remove();
    if (onCancel) onCancel();
  });
  btnGroup.appendChild(cancelBtn);

  form.appendChild(btnGroup);
  modal.appendChild(form);
  overlay.appendChild(modal);

  form.addEventListener('submit', (e) => {
    e.preventDefault();
    const errorEl = document.getElementById('recipe-form-error');
    if (errorEl) errorEl.hidden = true;

    const title = document.getElementById('title').value.trim();
    if (!title) {
      if (errorEl) {
        errorEl.textContent = 'タイトルは必須です';
        errorEl.hidden = false;
      }
      return;
    }

    const tagsStr = document.getElementById('tags').value.trim();
    const tags = tagsStr ? tagsStr.split(',').map(t => t.trim()).filter(Boolean) : [];

    const ingredientsStr = document.getElementById('ingredients').value.trim();
    const ingredients = ingredientsStr
      ? ingredientsStr.split('\n').filter(Boolean).map(line => {
          const parts = line.trim().split(/\s+/);
          return {
            name: parts[0] || '',
            quantity: parts[1] || null,
            unit: parts[2] || null,
          };
        })
      : [];

    const formData = {
      title,
      url: document.getElementById('url').value.trim() || null,
      imageUrl: document.getElementById('imageUrl').value.trim() || null,
      description: document.getElementById('description').value.trim() || null,
      tags,
      ingredients,
    };

    onSubmit(formData);
    overlay.remove();
  });

  // Close on overlay click
  overlay.addEventListener('click', (e) => {
    if (e.target === overlay) {
      overlay.remove();
      if (onCancel) onCancel();
    }
  });

  return overlay;
}

function createFormField(id, label, type, value, required) {
  const group = document.createElement('div');
  group.className = 'form-group';

  const labelEl = document.createElement('label');
  labelEl.setAttribute('for', id);
  labelEl.textContent = label;

  const input = document.createElement('input');
  input.type = type;
  input.id = id;
  input.name = id;
  input.value = value;
  if (required) input.required = true;

  group.appendChild(labelEl);
  group.appendChild(input);
  return group;
}
