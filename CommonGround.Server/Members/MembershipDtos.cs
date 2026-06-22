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

public sealed record MembershipPaymentDto(
    int Id,
    int AmountCents,
    string Currency,
    string Status,
    string Method,
    DateTime? PaidAtUtc,
    DateTime? PeriodStartUtc,
    DateTime? PeriodEndUtc,
    DateTime CreatedAtUtc);

internal static class MembershipPaymentMapping
{
    public static MembershipPaymentDto ToDto(this MembershipPayment p) =>
        new(p.Id, p.AmountCents, p.Currency, p.Status.ToString(), p.Method.ToString(),
            p.PaidAtUtc, p.PeriodStartUtc, p.PeriodEndUtc, p.CreatedAtUtc);
}
