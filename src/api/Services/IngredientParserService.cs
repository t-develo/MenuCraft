using System.Text.RegularExpressions;
using MenuCraft.Api.Dtos.Recipes;
using Microsoft.Extensions.Logging;

namespace MenuCraft.Api.Services;

public class IngredientParserService : IIngredientParserService
{
    private readonly ILogger<IngredientParserService> _logger;

    public IngredientParserService(ILogger<IngredientParserService> logger)
    {
        _logger = logger;
    }

    public ParseIngredientsResponse ParseIngredients(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ParseIngredientsResponse([]);

        var ingredients = text
            .Split('\n', StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseLine)
            .ToList();

        return new ParseIngredientsResponse(ingredients);
    }

    /// <summary>
    /// Parses a single ingredient line into name, quantity, and unit.
    /// Format: "名前 [数量] [単位]"
    /// Parts are split by whitespace; the first token is always the name.
    /// </summary>
    private static IngredientResponse ParseLine(string line)
    {
        var parts = Regex.Split(line.Trim(), @"\s+");

        var name = parts[0];
        string? quantity = parts.Length > 1 ? parts[1] : null;
        string? unit = parts.Length > 2 ? parts[2] : null;

        return new IngredientResponse(name, quantity, unit);
    }
}
