using System.Reflection;
using Resend;

namespace CommonGround.Server.Tests.Fakes;

/// <summary>
/// An <see cref="IResend"/> stand-in built with <see cref="DispatchProxy"/> (the interface has ~100
/// members). Records every message passed to <c>EmailSendAsync</c> and, when <see cref="FailWith"/>
/// returns an exception for a message, throws it instead - e.g. a <see cref="ResendException"/> with
/// <see cref="ErrorType.DailyQuotaExceeded"/> to simulate Resend's cap being hit.
/// </summary>
public class FakeResend : DispatchProxy
{
    private readonly List<EmailMessage> _sent = [];
    private readonly List<string> _idempotencyKeys = [];

    public IReadOnlyList<string> IdempotencyKeys
    {
        get { lock (_sent) return _idempotencyKeys.ToList(); }
    }

    public IReadOnlyList<EmailMessage> Sent
    {
        get { lock (_sent) return _sent.ToList(); }
    }

    public Func<EmailMessage, Exception?> FailWith { get; set; } = _ => null;

    public static IResend Create(out FakeResend fake)
    {
        var proxy = Create<IResend, FakeResend>();
        fake = (FakeResend)(object)proxy;
        return proxy;
    }

    public static ResendException AlreadySent() =>
        new(System.Net.HttpStatusCode.UnprocessableEntity, ErrorType.InvalidIdempotentRequest, "Same idempotency key used with a different request payload.", null);

    public static ResendException QuotaExceeded() =>
        new(System.Net.HttpStatusCode.TooManyRequests, ErrorType.DailyQuotaExceeded, "You have reached your daily email sending quota.", null);

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod!.Name == nameof(IResend.EmailSendAsync))
        {
            var message = args!.OfType<EmailMessage>().Single();
            if (FailWith(message) is { } failure)
            {
                return FaultedTaskFor(targetMethod.ReturnType, failure);
            }
            lock (_sent)
            {
                _sent.Add(message);
                _idempotencyKeys.AddRange(args!.OfType<string>());
            }
        }

        return CompletedTaskFor(targetMethod.ReturnType);
    }

    private static object CompletedTaskFor(Type returnType)
    {
        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            return Task.CompletedTask;
        }
        var resultType = returnType.GetGenericArguments()[0];
        var result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
        return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, [result])!;
    }

    private static object FaultedTaskFor(Type returnType, Exception failure)
    {
        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            return Task.FromException(failure);
        }
        var resultType = returnType.GetGenericArguments()[0];
        var fromException = typeof(Task).GetMethods()
            .Single(m => m.Name == nameof(Task.FromException) && m.IsGenericMethodDefinition);
        return fromException.MakeGenericMethod(resultType).Invoke(null, [failure])!;
    }
}

/// <summary>A <see cref="TimeProvider"/> whose clock only moves when a test advances it.</summary>
public sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
