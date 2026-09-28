using System.ComponentModel.DataAnnotations;

namespace Awai.Models.Entities
{
    /// <summary>
    /// One checkout. The same IdempotencyKey always returns this row, so a second click does not start another charge.
    /// </summary>
    public class PaymentIntent : BaseEntity
    {
        [StringLength(80)]
        public string IdempotencyKey { get; set; } = string.Empty;

        [StringLength(80)]
        public string Reference { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "LYD";

        public PaymentStatus Status { get; set; } = PaymentStatus.Queued;

        public int AttemptCount { get; set; }

        public DateTime? ProcessingStartedUtc { get; set; }

        [StringLength(120)]
        public string? ProviderReference { get; set; }

        [StringLength(500)]
        public string? LastError { get; set; }
    }
}
