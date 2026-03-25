using MenuCraft.Api.Dtos.Recipes;

namespace MenuCraft.Api.Services;

public interface IIngredientParserService
{
    /// <summary>
    /// Parses a multi-line text into a list of ingredients.
    /// Each line is expected to be in the format: "名前 [数量] [単位]"
    /// </summary>
    ParseIngredientsResponse ParseIngredients(string text);
}
