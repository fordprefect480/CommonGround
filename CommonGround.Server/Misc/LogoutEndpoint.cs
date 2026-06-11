using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;

namespace CommonGround.Server.Misc;

public sealed class LogoutEndpoint(SignInManager<ApplicationUser> signInManager)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/auth/logout");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await signInManager.SignOutAsync();
        await Send.NoContentAsync(ct);
    }
}
