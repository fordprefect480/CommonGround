namespace CommonGround.Server.LeasedBeds;

/// <summary>
/// Validation for leased-bed fees. Unlike membership, a bed fee may be $0
/// (a free term that skips payment), so the minimum is zero, not $1.
/// </summary>
public static class LeasedBedPrice
{
    /// <summary>Maximum allowed price in cents ($10,000.00).</summary>
    public const int MaxCents = 1_000_000;

    /// <summary>
    /// Validates a proposed fee in cents. Returns true when valid; otherwise sets
    /// <paramref name="error"/> to a plain-English message suitable for showing an admin.
    /// </summary>
    public static bool TryValidate(int cents, out string? error)
    {
        if (cents < 0)
        {
            error = "Price cannot be negative.";
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
