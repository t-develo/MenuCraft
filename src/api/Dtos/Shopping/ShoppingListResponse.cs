namespace MenuCraft.Api.Dtos.Shopping;

public record ShoppingListResponse(
    string WeekStart,
    IReadOnlyList<ShoppingItemResponse> Items
);
