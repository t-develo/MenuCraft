namespace MenuCraft.Api.Dtos;

public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Error
)
{
    public static ApiResponse<T> Ok(T data) => new(true, data, null);
    public static ApiResponse<T> Fail(string error) => new(false, default, error);
}

public record ApiResponse(
    bool Success,
    string? Error
)
{
    public static ApiResponse Ok() => new(true, null);
    public static ApiResponse Fail(string error) => new(false, error);
}
