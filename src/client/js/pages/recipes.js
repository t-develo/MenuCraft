'use strict';

/**
 * Recipes page - lists recipes and allows add/edit/delete.
 */

/**
 * Render the recipes page.
 * @param {HTMLElement} container - The main content container
 */
async function renderRecipesPage(container) {
  container.innerHTML = '';

  const header = document.createElement('div');
  header.className = 'page-header';

  const title = document.createElement('h2');
  title.textContent = 'レシピ一覧';
  header.appendChild(title);

  const addBtn = document.createElement('button');
  addBtn.className = 'btn btn-primary';
  addBtn.textContent = '+ レシピを追加';
  addBtn.addEventListener('click', () => {
    const form = createRecipeForm(null, {
      onSubmit: async (data) => {
        try {
          await RecipesApi.create(data);
          await renderRecipesPage(container);
        } catch (error) {
          console.error('Failed to create recipe:', error);
        }
      },
      onCancel: () => {},
    });
    document.body.appendChild(form);
  });
  header.appendChild(addBtn);

  container.appendChild(header);

  const listContainer = document.createElement('div');
  listContainer.id = 'recipe-list';
  listContainer.className = 'recipe-list';
  container.appendChild(listContainer);

  try {
    const recipes = await RecipesApi.getAll();

    if (recipes.length === 0) {
      const empty = document.createElement('p');
      empty.className = 'empty-state';
      empty.textContent = 'レシピがまだありません。「レシピを追加」ボタンで最初のレシピを登録しましょう！';
      listContainer.appendChild(empty);
      return;
    }

    for (const recipe of recipes) {
      const card = createRecipeCard(recipe, {
        onSelect: (r) => {
          const form = createRecipeForm(r, {
            onSubmit: async (data) => {
              try {
                await RecipesApi.update(r.id, data);
                await renderRecipesPage(container);
              } catch (error) {
                console.error('Failed to update recipe:', error);
              }
            },
            onCancel: () => {},
          });
          document.body.appendChild(form);
        },
        onDelete: async (id) => {
          if (confirm('このレシピを削除しますか？')) {
            try {
              await RecipesApi.delete(id);
              await renderRecipesPage(container);
            } catch (error) {
              console.error('Failed to delete recipe:', error);
            }
          }
        },
      });
      listContainer.appendChild(card);
    }
  } catch (error) {
    console.error('Failed to load recipes:', error);
    const errorEl = document.createElement('p');
    errorEl.className = 'error-message';
    errorEl.textContent = 'レシピの読み込みに失敗しました';
    listContainer.appendChild(errorEl);
  }
}
