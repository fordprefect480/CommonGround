namespace CommonGround.Server.Members;

/// <summary>
/// Validation rules for the editable annual membership price.
/// </summary>
public static class MembershipPrice
{
    /// <summary>Minimum allowed price in cents ($1.00).</summary>
    public const int MinCents = 100;

    /// <summary>Maximum allowed price in cents ($10,000.00).</summary>
    public const int MaxCents = 1_000_000;

    /// <summary>
    /// Validates a proposed price. Returns true when valid; otherwise sets
    /// <paramref name="error"/> to a plain-English message suitable for showing an admin.
    /// </summary>
    public static bool TryValidate(int cents, out string? error)
    {
        if (cents < MinCents)
        {
            error = $"Price must be at least ${MinCents / 100m:0.00}.";
            return false;
        }
        if (cents > MaxCents)
        {
            error = $"Price cannot be more than ${MaxCents / 100m:0.##}.";
            return false;
        }
        error = null;
        return true;
    }
}
