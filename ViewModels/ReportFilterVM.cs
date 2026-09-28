using Awai.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Awai.ViewModels
{
    public class ReportFilterVM
    {
        [Display(Name = "من تاريخ")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "إلى تاريخ")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        [Display(Name = "نوع التأمين")]
        public Guid? ProductId { get; set; }

        [Display(Name = "الحالة")]
        public ApplicationStatus? Status { get; set; }

        public List<InsuranceApplication> Results { get; set; } = [];

        public int TotalCount => Results.Count;
        public int SubmittedCount => Results.Count(a => a.Status == ApplicationStatus.Submitted);
        public int ReviewedCount => Results.Count(a => a.Status == ApplicationStatus.Reviewed);
        public int SyncedCount => Results.Count(a => a.Status == ApplicationStatus.Synced);
        public int FailedCount => Results.Count(a => a.Status == ApplicationStatus.Failed);
        public decimal PriceSum => Results.Sum(a => a.Product?.Price ?? 0);
    }
}
