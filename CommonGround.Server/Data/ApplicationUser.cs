using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CommonGround.Server.Data;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string? DisplayName { get; set; }
}
