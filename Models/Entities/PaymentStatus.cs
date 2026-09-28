namespace Awai.Models.Entities
{
    public enum PaymentStatus
    {
        Queued = 0,
        Processing = 1,
        Succeeded = 2,
        Failed = 3,
        NeedsReview = 4,
        AwaitingGateway = 5
    }

    public static class PaymentStatusText
    {
        public static string Arabic(this PaymentStatus status) => status switch
        {
            PaymentStatus.Queued => "بانتظار المعالجة",
            PaymentStatus.Processing => "قيد المعالجة",
            PaymentStatus.Succeeded => "تم الدفع",
            PaymentStatus.Failed => "فشل الدفع",
            PaymentStatus.NeedsReview => "تحتاج مراجعة قبل أي خصم جديد",
            PaymentStatus.AwaitingGateway => "بانتظار ربط بوابة الدفع",
            _ => status.ToString()
        };
    }
}
