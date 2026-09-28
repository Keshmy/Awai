namespace Awai.Services.Payments
{
    public class PaymentOptions
    {
        public bool Enabled { get; set; } = true;
        public string Redis { get; set; } = "localhost:6379";
        public string RabbitMq { get; set; } = "amqp://awai:awai-dev@localhost:5672/";
        public int StuckAfterSeconds { get; set; } = 120;
        public int MaxAttempts { get; set; } = 5;
        public int RetryDelaySeconds { get; set; } = 30;
        public int AcceptLockSeconds { get; set; } = 30;
    }
}
