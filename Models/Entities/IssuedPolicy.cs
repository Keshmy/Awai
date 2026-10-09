using System.ComponentModel.DataAnnotations;

namespace Awai.Models.Entities
{
    /// <summary>
    /// A compulsory (or agency) policy issued through this site. The customer can reopen it later with Id + AccessToken.
    /// </summary>
    public class IssuedPolicy : BaseEntity
    {
        [StringLength(64)]
        public string AccessToken { get; set; } = string.Empty;

        [StringLength(40)]
        public string ProductKind { get; set; } = "Compulsory";

        [StringLength(120)]
        public string? PolicyNumber { get; set; }

        [StringLength(120)]
        public string? TransactionCode { get; set; }

        [StringLength(1000)]
        public string? PdfUrl { get; set; }

        [StringLength(200)]
        public string InsuredName { get; set; } = string.Empty;

        [StringLength(40)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(40)]
        public string? PlateNumber { get; set; }

        [StringLength(80)]
        public string? NationalId { get; set; }

        public decimal? TotalPremium { get; set; }
    }
}
