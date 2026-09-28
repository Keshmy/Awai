using System.ComponentModel.DataAnnotations;

namespace Awai.ViewModels
{
    public class CompulsoryPolicyVM
    {
        [Required(ErrorMessage = "يرجى إدخال اسم المؤمن له")]
        [Display(Name = "اسم المؤمن له / المشترك")]
        public string InsuredName { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال الرقم الوطني أو جواز السفر")]
        [Display(Name = "الرقم الوطني / جواز السفر")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال رقم الهاتف")]
        [Display(Name = "رقم الهاتف")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "الجنسية")]
        public string Nationality { get; set; } = "ليبي";

        [Required(ErrorMessage = "يرجى إدخال اللوحة المعدنية بشكل صحيح")]
        [Display(Name = "اللوحة المعدنية")]
        [StringLength(30)]
        public string PlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال نوع المركبة")]
        [Display(Name = "النوع")]
        public string Make { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال لون المركبة")]
        [Display(Name = "اللون")]
        public string Color { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال رقم الهيكل")]
        [Display(Name = "رقم الهيكل")]
        public string ChassisNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال سنة صحيحة بين 1900 و 2050 (4 أرقام)")]
        [Range(1900, 2050, ErrorMessage = "يرجى إدخال سنة صحيحة بين 1900 و 2050 (4 أرقام)")]
        [Display(Name = "سنة الصنع")]
        public int? ManufactureYear { get; set; }

        [Display(Name = "جهة التسجيل")]
        public string? RegistrationAuthority { get; set; }

        [Required(ErrorMessage = "اختر نوع المركبة الرئيسي")]
        [Display(Name = "نوع المركبة الرئيسي - البند الرئيسي")]
        public string MainVehicleType { get; set; } = string.Empty;

        [Required(ErrorMessage = "اختر الموديل المحدد")]
        [Display(Name = "الموديل المحدد - تفاصيل البند")]
        public string ModelDetail { get; set; } = string.Empty;

        [Display(Name = "التفاصيل الإضافية (اختياري)")]
        public string? ExtraDetail { get; set; }

        [Display(Name = "عدد الركاب")]
        [Range(0, 100)]
        public int Passengers { get; set; }

        [Display(Name = "قوة المحرك (HP)")]
        [Range(0, 2000)]
        public int EngineHp { get; set; }

        [Display(Name = "الحمولة (طن) - لسيارات النقل والمقطورات التجارية")]
        [Range(0, 100)]
        public decimal LoadTons { get; set; }

        [Required]
        [Display(Name = "تاريخ البدء (بتوقيت ليبيا)")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Display(Name = "مدة الوثيقة / العقد")]
        public string CoverDuration { get; set; } = "annual";
    }
}
