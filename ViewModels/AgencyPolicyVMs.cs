using System.ComponentModel.DataAnnotations;

namespace Awai.ViewModels
{
    public class OrangeCardPolicyVM
    {
        [Required(ErrorMessage = "يرجى إدخال اسم المؤمن له")]
        [Display(Name = "اسم المؤمن له")]
        public string InsuredName { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال الرقم الوطني")]
        [Display(Name = "الرقم الوطني / جواز السفر")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال رقم الهاتف")]
        [Display(Name = "رقم الهاتف")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال اللوحة المعدنية")]
        [Display(Name = "اللوحة المعدنية")]
        public string PlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال رقم الهيكل")]
        [Display(Name = "رقم الهيكل")]
        public string ChassisNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال نوع المركبة")]
        [Display(Name = "نوع المركبة")]
        public string Make { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال سنة الصنع")]
        [Range(1900, 2050, ErrorMessage = "سنة بين 1900 و 2050")]
        [Display(Name = "سنة الصنع")]
        public int? ManufactureYear { get; set; }

        [Display(Name = "بلد السير / التسجيل")]
        public string? Country { get; set; } = "ليبيا";

        [Required]
        [Display(Name = "تاريخ البدء")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Display(Name = "مدة التغطية")]
        public string CoverDuration { get; set; } = "annual";
    }

    public class TravelersPolicyVM
    {
        [Required(ErrorMessage = "يرجى إدخال اسم المسافر")]
        [Display(Name = "اسم المسافر")]
        public string TravelerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال رقم جواز السفر")]
        [Display(Name = "رقم جواز السفر")]
        public string PassportNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال رقم الهاتف")]
        [Display(Name = "رقم الهاتف")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "الجنسية")]
        public string Nationality { get; set; } = "ليبي";

        [Required(ErrorMessage = "اختر وجهة السفر")]
        [Display(Name = "وجهة السفر")]
        public string Destination { get; set; } = string.Empty;

        [Required]
        [Display(Name = "تاريخ المغادرة")]
        [DataType(DataType.Date)]
        public DateTime DepartureDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "تاريخ العودة")]
        [DataType(DataType.Date)]
        public DateTime ReturnDate { get; set; } = DateTime.Today.AddDays(7);

        [Display(Name = "عدد أيام التغطية")]
        public int CoverDays { get; set; } = 7;

        [Display(Name = "نوع الرحلة")]
        public string TripType { get; set; } = "single";
    }
}
