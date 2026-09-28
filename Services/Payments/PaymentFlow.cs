using Awai.Models;
using Awai.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Awai.Services.Payments
{
    public class PaymentFlow : IPaymentFlow
    {
        private readonly AppDbContext _db;
        private readonly RedisPaymentLock _locks;
        private readonly RabbitPaymentBus _bus;
        private readonly ILogger<PaymentFlow> _logger;

        public PaymentFlow(AppDbContext db, RedisPaymentLock locks, RabbitPaymentBus bus, ILogger<PaymentFlow> logger)
        {
            _db = db;
            _locks = locks;
            _bus = bus;
            _logger = logger;
        }

        public Task<PaymentIntent?> FindAsync(Guid id, CancellationToken cancellationToken = default)
            => _db.PaymentIntents.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        public async Task<PaymentIntent> BeginAsync(BeginPaymentRequest request, CancellationToken cancellationToken = default)
        {
            var key = (request.IdempotencyKey ?? string.Empty).Trim();
            var reference = (request.Reference ?? string.Empty).Trim();
            if (key.Length is < 8 or > 80)
                throw new InvalidOperationException("مفتاح منع التكرار يجب أن يثبت في النموذج قبل أول ضغطة");
            if (string.IsNullOrWhiteSpace(reference))
                throw new InvalidOperationException("مرجع العملية مطلوب");
            if (request.Amount <= 0)
                throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");

            var existing = await _db.PaymentIntents.FirstOrDefaultAsync(p => p.IdempotencyKey == key, cancellationToken);
            if (existing != null)
                return existing;

            var locked = await _locks.TryAcquireAcceptAsync(key);
            if (!_locks.IsAvailable)
                throw new InvalidOperationException("قفل الدفع غير متاح. أعد المحاولة بعد اتصال Redis");
            if (!locked)
                return await WaitForExistingAsync(key, cancellationToken);

            existing = await _db.PaymentIntents.FirstOrDefaultAsync(p => p.IdempotencyKey == key, cancellationToken);
            if (existing != null)
                return existing;

            var payment = new PaymentIntent
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = key,
                Reference = reference,
                Amount = request.Amount,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "LYD" : request.Currency.Trim().ToUpperInvariant(),
                Status = PaymentStatus.Queued,
                Created = DateTime.UtcNow
            };
            _db.PaymentIntents.Add(payment);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _db.Entry(payment).State = EntityState.Detached;
                return await _db.PaymentIntents.FirstAsync(p => p.IdempotencyKey == key, cancellationToken);
            }

            try
            {
                await _bus.PublishProcessAsync(payment.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payment {PaymentId} was saved and will be queued when RabbitMQ reconnects", payment.Id);
            }

            return payment;
        }

        private async Task<PaymentIntent> WaitForExistingAsync(string key, CancellationToken cancellationToken)
        {
            for (var i = 0; i < 20; i++)
            {
                await Task.Delay(150, cancellationToken);
                var existing = await _db.PaymentIntents.FirstOrDefaultAsync(p => p.IdempotencyKey == key, cancellationToken);
                if (existing != null)
                    return existing;
            }

            throw new InvalidOperationException("طلب الدفع نفسه قيد التسجيل");
        }
    }
}
