using System.ComponentModel.DataAnnotations;

namespace Awai.Models.Entities
{
    /// <summary>
    /// Customer asks staff to resend a lost issued policy by WhatsApp or email.
    /// </summary>
    public class PolicySupportRequest : BaseEntity
    {
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(40)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(120)]
        public string? PolicyNumber { get; set; }

        [StringLength(80)]
        public string? NationalId { get; set; }

        [StringLength(40)]
        public string? PlateNumber { get; set; }

        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        public bool IsHandled { get; set; }

        public Guid? MatchedIssuedPolicyId { get; set; }
    }
}
