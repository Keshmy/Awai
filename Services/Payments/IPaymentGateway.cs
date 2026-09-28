namespace Awai.Services.Payments
{
    public class PaymentChargeRequest
    {
        public Guid PaymentId { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "LYD";
    }

    public class PaymentGatewayResult
    {
        public bool Succeeded { get; set; }
        public bool TransientFailure { get; set; }
        public string? ProviderReference { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// The bank gateway plugs in here later. ChargeAsync must send IdempotencyKey to the bank
    /// so a retry after a crash does not capture the same money twice.
    /// </summary>
    public interface IPaymentGateway
    {
        bool IsConfigured { get; }
        Task<PaymentGatewayResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default);
    }

    public class UnconfiguredPaymentGateway : IPaymentGateway
    {
        public bool IsConfigured => false;

        public Task<PaymentGatewayResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new PaymentGatewayResult
            {
                Succeeded = false,
                Message = "بوابة الدفع غير مربوطة بعد"
            });
    }
}
