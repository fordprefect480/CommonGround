using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CommonGround.Server.Data;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string? DisplayName => Compose(FirstName, LastName);

    public static string? Compose(string? firstName, string? lastName)
    {
        var first = string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim();
        var last = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim();
        if (first is null && last is null) return null;
        return string.Join(" ", new[] { first, last }.Where(s => s is not null));
    }
}
