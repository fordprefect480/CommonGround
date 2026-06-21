using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommonGround.Server.Data;

/// <summary>
/// A physical garden bed in the leasing inventory. Identified by section
/// (<c>N</c> or <c>S</c>) plus a number that is unique within its section.
/// Beds are taken out of service via <see cref="IsActive"/> rather than deleted
/// so historical leases keep their foreign key.
/// </summary>
public class Bed
{
    public int Id { get; set; }

    [MaxLength(1)]
    public string Section { get; set; } = "";

    public int Number { get; set; }

    [MaxLength(50)]
    public string? Label { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public ICollection<BedLease> Leases { get; set; } = new List<BedLease>();

    /// <summary>Display code such as <c>N1</c> or <c>S16</c>.</summary>
    [NotMapped]
    public string Code => $"{Section}{Number}";
}
