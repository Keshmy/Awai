using System.ComponentModel.DataAnnotations;

namespace Awai.Models.Entities
{
    public class StaffNotification : BaseEntity
    {
        public Guid ApplicationId { get; set; }
        public InsuranceApplication? Application { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = "طلب تأمين جديد";

        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        public bool IsDismissed { get; set; }

        public DateTime? DismissedAt { get; set; }

        [MaxLength(450)]
        public string? DismissedByUserId { get; set; }
    }
}
