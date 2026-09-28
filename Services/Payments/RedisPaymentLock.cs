using StackExchange.Redis;

namespace Awai.Services.Payments
{
    public class RedisPaymentLock
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly PaymentOptions _options;

        public RedisPaymentLock(IConnectionMultiplexer redis, Microsoft.Extensions.Options.IOptions<PaymentOptions> options)
        {
            _redis = redis;
            _options = options.Value;
        }

        public bool IsAvailable => _redis.IsConnected;

        public async Task<bool> TryAcquireAcceptAsync(string idempotencyKey)
        {
            if (!_redis.IsConnected)
                return false;
            return await _redis.GetDatabase().StringSetAsync(
                $"payment:accept:{idempotencyKey}",
                "1",
                TimeSpan.FromSeconds(Math.Max(5, _options.AcceptLockSeconds)),
                When.NotExists);
        }

        public async Task<bool> TryAcquireProcessingAsync(Guid paymentId)
        {
            if (!_redis.IsConnected)
                return false;
            return await _redis.GetDatabase().StringSetAsync(
                ProcessingKey(paymentId),
                Environment.MachineName,
                TimeSpan.FromSeconds(Math.Max(30, _options.StuckAfterSeconds)),
                When.NotExists);
        }

        public async Task<bool> ProcessingLockHeldAsync(Guid paymentId)
        {
            if (!_redis.IsConnected)
                return true;
            return await _redis.GetDatabase().KeyExistsAsync(ProcessingKey(paymentId));
        }

        public async Task ReleaseProcessingAsync(Guid paymentId)
        {
            if (!_redis.IsConnected)
                return;
            await _redis.GetDatabase().KeyDeleteAsync(ProcessingKey(paymentId));
        }

        public async Task<bool> TryMarkRequeuedAsync(Guid paymentId)
        {
            if (!_redis.IsConnected)
                return false;
            return await _redis.GetDatabase().StringSetAsync(
                $"payment:requeue:{paymentId}",
                "1",
                TimeSpan.FromSeconds(Math.Max(30, _options.RetryDelaySeconds)),
                When.NotExists);
        }

        private static string ProcessingKey(Guid paymentId) => $"payment:processing:{paymentId}";
    }
}
