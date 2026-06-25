using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;

namespace CommonGround.Server.Members;

public sealed class ListMemberPaymentsEndpoint(AppDbContext db)
    : Endpoint<ListMemberPaymentsEndpoint.Request, List<PaymentDto>>
{
    public sealed class Request
    {
        public string Id { get; set; } = "";
    }

    public override void Configure()
    {
        Get("/members/{id}/payments");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // Admins also see failed payments (they help with "I paid but it didn't work" support);
        // abandoned/expired checkouts (Pending) are excluded as noise.
        var payments = await PaymentHistory.LoadAsync(db, req.Id, includeFailed: true, ct);

        await Send.OkAsync(payments, ct);
    }
}
