namespace MenuCraft.Api.Dtos.Shopping;

public record UpdateCheckRequest(
    string WeekStart,
    string IngredientName,
    bool IsChecked
);
