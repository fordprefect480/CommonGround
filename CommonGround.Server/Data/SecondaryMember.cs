using System.ComponentModel.DataAnnotations;

namespace CommonGround.Server.Data;

public class SecondaryMember
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public ApplicationUser? User { get; set; }

    [MaxLength(200)]
    public string FullName { get; set; } = "";
}
