namespace MenuCraft.Api.Dtos.Shopping;

public record ShoppingItemResponse(
    string IngredientName,
    string? TotalQuantity,
    string? Unit,
    bool IsChecked,
    IReadOnlyList<ShoppingItemSource> Sources
);
