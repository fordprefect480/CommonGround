using System.Reflection;
using Resend;

namespace CommonGround.Server.Email;

/// <summary>
/// A development-only <see cref="IResend"/> wrapper that reroutes every outgoing email
/// to a single safe address, so local runs can exercise real delivery without mailing
/// actual members. Any argument that is an <see cref="EmailMessage"/> (or a batch of
/// them) has its To/Cc/Bcc replaced with the redirect address before the call reaches
/// the real client; every other API call passes through untouched.
/// </summary>
/// <remarks>
/// Implemented as a <see cref="DispatchProxy"/> because <see cref="IResend"/> exposes
/// ~100 members and only the send operations (<c>EmailSendAsync</c>, <c>EmailBatchAsync</c>)
/// need intercepting; rewriting by argument type rather than method name keeps it robust
/// as the surface evolves. Registered only in Development, so it can never fire in production.
/// Cannot be sealed: <see cref="DispatchProxy.Create{T, TProxy}"/> generates a runtime subclass.
/// </remarks>
public class RedirectingResendProxy : DispatchProxy
{
    private IResend _inner = null!;
    private string _redirectTo = null!;
    private ILogger _logger = null!;

    /// <summary>Wraps <paramref name="inner"/> so all outgoing mail is rerouted to <paramref name="redirectTo"/>.</summary>
    public static IResend Wrap(IResend inner, string redirectTo, ILogger logger)
    {
        var proxy = Create<IResend, RedirectingResendProxy>();
        var typed = (RedirectingResendProxy)(object)proxy;
        typed._inner = inner;
        typed._redirectTo = redirectTo;
        typed._logger = logger;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (args is not null)
        {
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case EmailMessage message:
                        Redirect(message);
                        break;
                    case IEnumerable<EmailMessage> batch:
                        foreach (var message in batch)
                        {
                            Redirect(message);
                        }
                        break;
                }
            }
        }

        return targetMethod!.Invoke(_inner, args);
    }

    private void Redirect(EmailMessage message)
    {
        _logger.LogWarning(
            "Local email redirect: rerouting \"{Subject}\" from [{Original}] to {RedirectTo}",
            message.Subject, string.Join(", ", message.To), _redirectTo);

        message.To = new EmailAddressList { _redirectTo };
        message.Cc = null;
        message.Bcc = null;
    }
}
