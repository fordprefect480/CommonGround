using CommonGround.Server.Activity;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Email;

/// <summary>
/// Finishes bulk sends that were larger than the day's Resend allowance: every
/// <see cref="Interval"/> it hands whatever allowance has freed up to
/// <see cref="BulkEmailDispatcher"/> and records the progress in the activity log.
/// </summary>
public sealed class BulkEmailQueueWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> options,
    TimeProvider time,
    ILogger<BulkEmailQueueWorker> logger)
    : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Queued email dispatch failed; will retry in {Interval}", Interval);
            }
        }
    }

    /// <summary>One dispatch pass. Public so tests can drive it without waiting on the timer.</summary>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        if (!options.Value.IsConfigured)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<BulkEmailDispatcher>();
        var activityLogger = scope.ServiceProvider.GetRequiredService<IActivityLogger>();

        var progress = await dispatcher.DispatchQueuedAsync(ct);
        foreach (var email in progress.Where(p => p.SentThisPass > 0))
        {
            var summary = email.QueuedRemaining == 0
                ? $"Finished sending \"{email.Subject}\": the last {email.SentThisPass} went out ({email.SentTotal} of {email.RecipientCount} delivered)"
                : $"Sent the next {email.SentThisPass} of \"{email.Subject}\" ({email.SentTotal} of {email.RecipientCount} delivered, {email.QueuedRemaining} still waiting)";

            await activityLogger.LogAsync(
                "email.queued_batch_sent",
                summary,
                targetType: "sentEmail",
                targetId: email.SentEmailId.ToString(),
                details: email,
                ct: ct);
        }
    }
}
