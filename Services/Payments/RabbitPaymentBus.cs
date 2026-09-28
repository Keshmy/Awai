using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Awai.Services.Payments
{
    public class RabbitPaymentBus : IAsyncDisposable
    {
        public const string Exchange = "awai.payments";
        public const string ProcessQueue = "awai.payments.process";
        public const string RetryQueue = "awai.payments.retry";
        public const string DeadQueue = "awai.payments.dead";
        public const string ProcessKey = "process";
        public const string RetryKey = "retry";
        public const string DeadKey = "dead";

        private readonly PaymentOptions _options;
        private readonly ILogger<RabbitPaymentBus> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private IConnection? _connection;
        private IChannel? _publisher;

        public RabbitPaymentBus(IOptions<PaymentOptions> options, ILogger<RabbitPaymentBus> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConnected => _connection?.IsOpen == true;

        public async Task EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (IsConnected && _publisher?.IsOpen == true)
                return;

            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (IsConnected && _publisher?.IsOpen == true)
                    return;

                await DisposeChannelsAsync();
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_options.RabbitMq),
                    AutomaticRecoveryEnabled = true
                };
                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _publisher = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                await DeclareAsync(_publisher, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<IChannel> OpenConsumerChannelAsync(CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            var channel = await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);
            await DeclareAsync(channel, cancellationToken);
            await channel.BasicQosAsync(0, 1, false, cancellationToken);
            return channel;
        }

        public Task PublishProcessAsync(Guid paymentId, CancellationToken cancellationToken)
            => PublishAsync(ProcessKey, paymentId, cancellationToken);

        public Task PublishRetryAsync(Guid paymentId, CancellationToken cancellationToken)
            => PublishAsync(RetryKey, paymentId, cancellationToken);

        public async ValueTask DisposeAsync()
        {
            await DisposeChannelsAsync();
            _gate.Dispose();
        }

        private async Task PublishAsync(string routingKey, Guid paymentId, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new PaymentMessage(paymentId)));
            var props = new BasicProperties { Persistent = true };
            await _publisher!.BasicPublishAsync(Exchange, routingKey, false, props, body, cancellationToken);
            _logger.LogInformation("Payment {PaymentId} published to {RoutingKey}", paymentId, routingKey);
        }

        private async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
        {
            await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(DeadQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
            await channel.QueueBindAsync(DeadQueue, Exchange, DeadKey, cancellationToken: cancellationToken);

            var retryArgs = new Dictionary<string, object?>
            {
                ["x-message-ttl"] = Math.Max(5, _options.RetryDelaySeconds) * 1000,
                ["x-dead-letter-exchange"] = Exchange,
                ["x-dead-letter-routing-key"] = ProcessKey
            };
            await channel.QueueDeclareAsync(RetryQueue, durable: true, exclusive: false, autoDelete: false, arguments: retryArgs, cancellationToken: cancellationToken);
            await channel.QueueBindAsync(RetryQueue, Exchange, RetryKey, cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(ProcessQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
            await channel.QueueBindAsync(ProcessQueue, Exchange, ProcessKey, cancellationToken: cancellationToken);
        }

        private async Task DisposeChannelsAsync()
        {
            if (_publisher != null)
            {
                await _publisher.DisposeAsync();
                _publisher = null;
            }
            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        private sealed record PaymentMessage(Guid PaymentId);
    }
}
