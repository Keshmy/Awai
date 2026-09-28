using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Awai.Services.Payments
{
    public class PaymentWorkerHost : BackgroundService
    {
        private readonly RabbitPaymentBus _bus;
        private readonly IServiceScopeFactory _scopes;
        private readonly PaymentOptions _options;
        private readonly ILogger<PaymentWorkerHost> _logger;

        public PaymentWorkerHost(
            RabbitPaymentBus bus,
            IServiceScopeFactory scopes,
            Microsoft.Extensions.Options.IOptions<PaymentOptions> options,
            ILogger<PaymentWorkerHost> logger)
        {
            _bus = bus;
            _scopes = scopes;
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
                    await ConsumeAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Payment worker lost RabbitMQ. Retrying in 10 seconds");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task ConsumeAsync(CancellationToken stoppingToken)
        {
            await using var channel = await _bus.OpenConsumerChannelAsync(stoppingToken);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, delivery) =>
            {
                try
                {
                    var message = JsonSerializer.Deserialize<PaymentMessage>(Encoding.UTF8.GetString(delivery.Body.Span));
                    if (message != null)
                    {
                        using var scope = _scopes.CreateScope();
                        var processor = scope.ServiceProvider.GetRequiredService<PaymentProcessor>();
                        await processor.HandleAsync(message.PaymentId, stoppingToken);
                    }
                    await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Payment message could not be handled");
                    await channel.BasicNackAsync(delivery.DeliveryTag, false, requeue: false, stoppingToken);
                }
            };

            await channel.BasicConsumeAsync(RabbitPaymentBus.ProcessQueue, autoAck: false, consumer, stoppingToken);
            _logger.LogInformation("Payment worker is listening");
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private sealed record PaymentMessage(Guid PaymentId);
    }
}
