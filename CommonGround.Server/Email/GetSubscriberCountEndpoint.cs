using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Email;

public sealed class GetSubscriberCountEndpoint(AppDbContext db)
    : EndpointWithoutRequest<GetSubscriberCountEndpoint.Result>
{
    public sealed record Result(int Count);

    public override void Configure()
    {
        Get("/email/subscribers/count");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var count = await db.Users
            .Where(u => u.IsSubscribedToMailingList && u.Email != null && u.Email != "")
            .CountAsync(ct);

        await Send.OkAsync(new Result(count), ct);
    }
}
