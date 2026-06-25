using CommonGround.Server.Data;

namespace CommonGround.Server.Members;

public sealed record SignupRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? Address,
    string Password,
    string[]? SecondaryMembers,
    bool SubscribeNewsletter,
    string? CaptchaToken);

public sealed record SignupResult(string? CheckoutUrl);

public sealed record CompleteRequest(string SessionId);

public sealed record CompleteResult(bool Ok);

// A single payment row in a member's history. Covers both membership payments (which carry a
// period) and leased-bed payments (which carry the bed label); Kind tells them apart.
public sealed record PaymentDto(
    int Id,
    string Kind,
    int AmountCents,
    string Currency,
    string Status,
    string Method,
    DateTime? PaidAtUtc,
    DateTime? PeriodStartUtc,
    DateTime? PeriodEndUtc,
    string? BedLabel,
    DateTime CreatedAtUtc);

internal static class PaymentMapping
{
    public static PaymentDto ToDto(this MembershipPayment p) =>
        new(p.Id, "Membership", p.AmountCents, p.Currency, p.Status.ToString(), p.Method.ToString(),
            p.PaidAtUtc, p.PeriodStartUtc, p.PeriodEndUtc, null, p.CreatedAtUtc);

    public static PaymentDto ToDto(this BedLeasePayment p, string? bedLabel) =>
        new(p.Id, "LeasedBed", p.AmountCents, p.Currency, p.Status.ToString(), p.Method.ToString(),
            p.PaidAtUtc, null, null, bedLabel, p.CreatedAtUtc);
}
