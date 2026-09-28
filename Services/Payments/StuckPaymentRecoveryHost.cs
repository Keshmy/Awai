using Awai.Models;
using Awai.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Awai.Services.Payments
{
    /// <summary>
    /// A worker that dies mid-charge leaves the row in Processing. This puts it back on the queue
    /// once the Redis lock has expired. After the attempt limit it stops and waits for a person.
    /// </summary>
    public class StuckPaymentRecoveryHost : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly RabbitPaymentBus _bus;
        private readonly RedisPaymentLock _locks;
        private readonly PaymentOptions _options;
        private readonly ILogger<StuckPaymentRecoveryHost> _logger;

        public StuckPaymentRecoveryHost(
            IServiceScopeFactory scopes,
            RabbitPaymentBus bus,
            RedisPaymentLock locks,
            Microsoft.Extensions.Options.IOptions<PaymentOptions> options,
            ILogger<StuckPaymentRecoveryHost> logger)
        {
            _scopes = scopes;
            _bus = bus;
            _locks = locks;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
                return;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RecoverAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Stuck payment recovery failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task RecoverAsync(CancellationToken cancellationToken)
        {
            if (!_locks.IsAvailable || !_bus.IsConnected)
                return;

            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stuckBefore = DateTime.UtcNow.AddSeconds(-Math.Max(30, _options.StuckAfterSeconds));
            var lostBefore = DateTime.UtcNow.AddSeconds(-Math.Max(45, _options.RetryDelaySeconds + 15));

            var stuck = await db.PaymentIntents
                .Where(p => p.Status == PaymentStatus.Processing && p.ProcessingStartedUtc != null && p.ProcessingStartedUtc < stuckBefore)
                .ToListAsync(cancellationToken);

            var lost = await db.PaymentIntents
                .Where(p => p.Status == PaymentStatus.Queued && (p.Modified ?? p.Created) < lostBefore)
                .ToListAsync(cancellationToken);

            foreach (var payment in stuck)
            {
                if (await _locks.ProcessingLockHeldAsync(payment.Id))
                    continue;
                if (!await _locks.TryMarkRequeuedAsync(payment.Id))
                    continue;

                if (payment.AttemptCount >= _options.MaxAttempts)
                {
                    payment.Status = PaymentStatus.NeedsReview;
                    payment.LastError = "توقفت المعالجة. راجع العملية لدى البنك قبل إعادة الخصم.";
                    payment.Modified = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                    _logger.LogWarning("Payment {PaymentId} needs review after a stuck charge", payment.Id);
                    continue;
                }

                payment.Status = PaymentStatus.Queued;
                payment.Modified = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                await _bus.PublishProcessAsync(payment.Id, cancellationToken);
                _logger.LogInformation("Requeued stuck payment {PaymentId}", payment.Id);
            }

            foreach (var payment in lost)
            {
                if (!await _locks.TryMarkRequeuedAsync(payment.Id))
                    continue;
                await _bus.PublishProcessAsync(payment.Id, cancellationToken);
                _logger.LogInformation("Requeued payment {PaymentId} that never reached a worker", payment.Id);
            }
        }
    }
}
