using Microsoft.AspNetCore.Identity;

namespace MenuCraft.Api.Models;

public class User : IdentityUser<Guid>
{
    public int? FamilyGroupId { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public FamilyGroup? FamilyGroup { get; init; }
}
