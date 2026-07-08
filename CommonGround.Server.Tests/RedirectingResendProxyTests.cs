using System.Reflection;
using CommonGround.Server.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Resend;

namespace CommonGround.Server.Tests;

public class RedirectingResendProxyTests
{
    private const string Redirect = "redirect@example.com";

    private static IResend Wrap(out RecordingResend recorder)
    {
        var inner = RecordingResend.Build(out recorder);
        return RedirectingResendProxy.Wrap(inner, Redirect, NullLogger<RedirectingResendProxy>.Instance);
    }

    private static EmailMessage Message(string to)
    {
        var message = new EmailMessage { From = "garden@example.org", Subject = "Hello" };
        message.To.Add(to);
        return message;
    }

    [Fact]
    public async Task EmailSendAsync_reroutes_recipients_and_clears_cc_bcc()
    {
        var proxy = Wrap(out _);
        var message = Message("member@example.org");
        message.Cc = new EmailAddressList { "cc@example.org" };
        message.Bcc = new EmailAddressList { "bcc@example.org" };

        await proxy.EmailSendAsync(message, default);

        Assert.Equal([Redirect], message.To.Select(a => a.Email));
        Assert.Null(message.Cc);
        Assert.Null(message.Bcc);
    }

    [Fact]
    public async Task EmailSendAsync_idempotency_overload_is_also_redirected()
    {
        var proxy = Wrap(out _);
        var message = Message("member@example.org");

        await proxy.EmailSendAsync("idempotency-key", message, default);

        Assert.Equal([Redirect], message.To.Select(a => a.Email));
    }

    [Fact]
    public async Task EmailBatchAsync_reroutes_every_message()
    {
        var proxy = Wrap(out _);
        var first = Message("a@example.org");
        var second = Message("b@example.org");

        await proxy.EmailBatchAsync([first, second], default);

        Assert.Equal([Redirect], first.To.Select(a => a.Email));
        Assert.Equal([Redirect], second.To.Select(a => a.Email));
    }

    [Fact]
    public async Task Non_email_calls_pass_through_untouched()
    {
        var proxy = Wrap(out var recorder);

        await proxy.DomainListAsync(default);

        Assert.Contains("DomainListAsync", recorder.Calls);
        Assert.Empty(recorder.Received);
    }

    /// <summary>
    /// A minimal recording <see cref="IResend"/> built with <see cref="DispatchProxy"/> so the
    /// ~100-member interface needn't be hand-implemented: it records the method names it sees and
    /// any <see cref="EmailMessage"/> arguments, then returns a completed task of the declared type.
    /// </summary>
    private class RecordingResend : DispatchProxy
    {
        public List<string> Calls { get; } = [];
        public List<EmailMessage> Received { get; } = [];

        public static IResend Build(out RecordingResend recorder)
        {
            var proxy = Create<IResend, RecordingResend>();
            recorder = (RecordingResend)(object)proxy;
            return (IResend)proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls.Add(targetMethod!.Name);
            foreach (var arg in args ?? [])
            {
                if (arg is EmailMessage message)
                {
                    Received.Add(message);
                }
                else if (arg is IEnumerable<EmailMessage> batch)
                {
                    Received.AddRange(batch);
                }
            }

            return CompletedTaskFor(targetMethod.ReturnType);
        }

        private static object CompletedTaskFor(Type returnType)
        {
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = returnType.GetGenericArguments()[0];
                var result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
                return typeof(Task).GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(resultType)
                    .Invoke(null, [result])!;
            }

            return Task.CompletedTask;
        }
    }
}
