using Awai.Models;
using Awai.Models.Entities;

namespace Awai.Services.Payments
{
    public class PaymentProcessor
    {
        private readonly AppDbContext _db;
        private readonly RedisPaymentLock _locks;
        private readonly RabbitPaymentBus _bus;
        private readonly IPaymentGateway _gateway;
        private readonly PaymentOptions _options;
        private readonly ILogger<PaymentProcessor> _logger;

        public PaymentProcessor(
            AppDbContext db,
            RedisPaymentLock locks,
            RabbitPaymentBus bus,
            IPaymentGateway gateway,
            Microsoft.Extensions.Options.IOptions<PaymentOptions> options,
            ILogger<PaymentProcessor> logger)
        {
            _db = db;
            _locks = locks;
            _bus = bus;
            _gateway = gateway;
            _options = options.Value;
            _logger = logger;
        }

        public async Task HandleAsync(Guid paymentId, CancellationToken cancellationToken)
        {
            var payment = await _db.PaymentIntents.FindAsync([paymentId], cancellationToken);
            if (payment == null || IsClosed(payment.Status))
                return;

            if (!_locks.IsAvailable)
            {
                _logger.LogWarning("Redis is down. Payment {PaymentId} stays queued so it is not charged twice", paymentId);
                return;
            }

            if (!await _locks.TryAcquireProcessingAsync(paymentId))
                return;

            try
            {
                payment = await _db.PaymentIntents.FindAsync([paymentId], cancellationToken);
                if (payment == null || IsClosed(payment.Status))
                    return;

                if (!_gateway.IsConfigured)
                {
                    payment.Status = PaymentStatus.AwaitingGateway;
                    payment.LastError = "بوابة الدفع غير مربوطة بعد";
                    payment.Modified = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);
                    return;
                }

                if (payment.AttemptCount >= _options.MaxAttempts)
                {
                    MarkReview(payment, "استنفدت المحاولات. راجع العملية لدى البنك قبل إعادة الخصم.");
                    await _db.SaveChangesAsync(cancellationToken);
                    return;
                }

                payment.Status = PaymentStatus.Processing;
                payment.AttemptCount += 1;
                payment.ProcessingStartedUtc = DateTime.UtcNow;
                payment.Modified = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);

                PaymentGatewayResult result;
                try
                {
                    result = await _gateway.ChargeAsync(new PaymentChargeRequest
                    {
                        PaymentId = payment.Id,
                        IdempotencyKey = payment.IdempotencyKey,
                        Reference = payment.Reference,
                        Amount = payment.Amount,
                        Currency = payment.Currency
                    }, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Payment {PaymentId} failed while calling the gateway", payment.Id);
                    result = new PaymentGatewayResult { TransientFailure = true, Message = "تعذر الاتصال ببوابة الدفع" };
                }

                if (result.Succeeded)
                {
                    payment.Status = PaymentStatus.Succeeded;
                    payment.ProviderReference = result.ProviderReference;
                    payment.LastError = null;
                }
                else if (result.TransientFailure && payment.AttemptCount < _options.MaxAttempts)
                {
                    payment.Status = PaymentStatus.Queued;
                    payment.LastError = result.Message;
                    payment.Modified = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);
                    await _bus.PublishRetryAsync(payment.Id, cancellationToken);
                    return;
                }
                else if (result.TransientFailure)
                {
                    MarkReview(payment, result.Message ?? "تعذر إكمال الدفع بعد عدة محاولات");
                }
                else
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.LastError = result.Message;
                }

                payment.Modified = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                await _locks.ReleaseProcessingAsync(paymentId);
            }
        }

        private static bool IsClosed(PaymentStatus status)
            => status is PaymentStatus.Succeeded or PaymentStatus.Failed or PaymentStatus.NeedsReview or PaymentStatus.AwaitingGateway;

        private static void MarkReview(PaymentIntent payment, string message)
        {
            payment.Status = PaymentStatus.NeedsReview;
            payment.LastError = message;
            payment.Modified = DateTime.UtcNow;
        }
    }
}
