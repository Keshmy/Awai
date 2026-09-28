using Awai.Models.Entities;

namespace Awai.Services.Payments
{
    public class BeginPaymentRequest
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "LYD";
    }

    public interface IPaymentFlow
    {
        Task<PaymentIntent> BeginAsync(BeginPaymentRequest request, CancellationToken cancellationToken = default);
        Task<PaymentIntent?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
