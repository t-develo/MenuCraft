using System.ComponentModel.DataAnnotations;

namespace MenuCraft.Api.Dtos.Recipes;

public record FetchOgpRequest(
    [Required][MaxLength(2000)] string Url
);
