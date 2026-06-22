using System.ComponentModel.DataAnnotations;

namespace CommonGround.Server.Data;

/// <summary>
/// A physical garden bed in the leasing inventory, identified by a free-form
/// admin-assigned <see cref="Label"/> (e.g. <c>N1</c> or <c>X2039</c>). Beds are
/// taken out of service via <see cref="IsActive"/>, or soft-deleted via
/// <see cref="IsDeleted"/>, so historical leases keep their foreign key.
/// </summary>
public class Bed
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Label { get; set; } = "";

    public bool IsActive { get; set; } = true;

    /// <summary>Soft-deleted beds are hidden from every listing but kept so historical leases keep their FK.</summary>
    public bool IsDeleted { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public ICollection<BedLease> Leases { get; set; } = new List<BedLease>();
}
