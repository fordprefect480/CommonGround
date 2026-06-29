using CommonGround.Server.Auth;
using FastEndpoints;

namespace CommonGround.Server.Members;

/// <summary>
/// Exposes the renewal target date - the paid-through date a membership payment
/// made now would reach (the next 1 July boundary, with the late-join carry-over).
/// The admin Members page uses it to single out members who haven't paid for the
/// upcoming year, since the Adelaide-local boundary can't be derived reliably in
/// the browser.
/// </summary>
public sealed class GetMembershipRenewalEndpoint
    : EndpointWithoutRequest<GetMembershipRenewalEndpoint.Result>
{
    public sealed record Result(DateTime RenewalTargetUtc);

    public override void Configure()
    {
        Get("/members/membership-renewal");
        Group<AdminGroup>();
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        var renewalTarget = MembershipPeriod.ComputePaidThrough(DateTime.UtcNow);
        return Send.OkAsync(new Result(renewalTarget), ct);
    }
}
