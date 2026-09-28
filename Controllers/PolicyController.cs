using Awai.Helpers;
using Awai.Services.Ims;
using Awai.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Awai.Controllers
{
    /// <summary>
    /// Agency policy pages. Compulsory cars calls LISA in Test mode.
    /// Orange card and travel stay local demos until those APIs are wired.
    /// </summary>
    public class PolicyController : Controller
    {
        private readonly ImsApiClient _ims;

        public PolicyController(ImsApiClient ims)
        {
            _ims = ims;
        }

        [HttpGet]
        public async Task<IActionResult> Compulsory()
        {
            ViewData["Title"] = "وثيقة جديدة - سيارات إجباري";
            ViewData["PolicyKind"] = PolicyProductRoutes.Compulsory;
            await LoadVehicleTypesAsync();
            return View(new CompulsoryPolicyVM());
        }

        [HttpGet]
        public async Task<IActionResult> SpecVehicles(Guid typeId)
        {
            if (!_ims.IsConfigured || typeId == Guid.Empty)
                return Json(Array.Empty<LisaSpecVehicle>());

            try
            {
                return Json(await _ims.GetSpecVehiclesAsync(typeId));
            }
            catch (ImsApiException ex)
            {
                return StatusCode((int)ex.StatusCode, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DetailVehicles(Guid typeId)
        {
            if (!_ims.IsConfigured || typeId == Guid.Empty)
                return Json(Array.Empty<LisaSpecVehicle>());

            try
            {
                return Json(await _ims.GetDetailVehiclesAsync(typeId));
            }
            catch (ImsApiException ex)
            {
                return StatusCode((int)ex.StatusCode, new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewCompulsory(CompulsoryPolicyVM model)
        {
            if (!ModelState.IsValid)
            {
                await LoadVehicleTypesAsync();
                return View("Compulsory", model);
            }

            if (!_ims.IsConfigured)
                return DemoResult(model, "Compulsory", "معاينة", null);

            var rejection = DescribeLisaRejection(model);
            if (rejection != null)
            {
                ModelState.AddModelError(string.Empty, rejection);
                await LoadVehicleTypesAsync();
                return View("Compulsory", model);
            }

            try
            {
                await _ims.EnsureSessionAsync();
                var quote = await _ims.InquiryAsync(BuildLisaBody(model));
                ViewBag.DemoMode = false;
                ViewBag.ActionLabel = "معاينة القسط";
                ViewBag.BackAction = "Compulsory";
                ViewBag.ApiMessage = quote.Message;
                ViewBag.Premium = quote;
                return View("Result", model);
            }
            catch (ImsApiException ex)
            {
                var detail = ex.StatusCode == System.Net.HttpStatusCode.BadRequest
                    ? DescribeLisaRejection(model) ?? "الاحتساب مرفوض. راجع الحقول المرسلة في سجل الخادم."
                    : ex.Message;
                ModelState.AddModelError(string.Empty, detail);
                await LoadVehicleTypesAsync();
                return View("Compulsory", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueCompulsory(CompulsoryPolicyVM model)
        {
            if (!ModelState.IsValid)
            {
                await LoadVehicleTypesAsync();
                return View("Compulsory", model);
            }

            if (!_ims.IsConfigured)
                return DemoResult(model, "Compulsory", "إصدار", $"DEMO-CAR-{DateTime.Now:yyyyMMddHHmmss}");

            try
            {
                await _ims.EnsureSessionAsync();
                var issued = await _ims.CreateAsync(BuildLisaBody(model));
                ViewBag.DemoMode = false;
                ViewBag.ActionLabel = "إصدار الوثيقة";
                ViewBag.BackAction = "Compulsory";
                ViewBag.ApiMessage = issued.Message;
                ViewBag.PolicyRef = issued.TransactionCode ?? issued.PolicyId;
                ViewBag.PdfUrl = issued.PdfUrl;
                if (issued.Premium != null)
                {
                    ViewBag.Premium = new LisaInquiryResponse
                    {
                        Success = issued.Success,
                        Message = issued.Message,
                        NetPremium = issued.Premium.NetPremium,
                        Tax = issued.Premium.Tax,
                        SupervisionFees = issued.Premium.SupervisionFees,
                        Stamp = issued.Premium.Stamp,
                        IssuingFees = issued.Premium.IssuingFees,
                        TotalPremium = issued.Premium.TotalPremium
                    };
                }
                return View("Result", model);
            }
            catch (ImsApiException ex)
            {
                ViewBag.DemoMode = false;
                ViewBag.IsError = true;
                ViewBag.ActionLabel = "إصدار الوثيقة";
                ViewBag.BackAction = "Compulsory";
                ViewBag.ApiMessage = ex.Message;
                return View("Result", model);
            }
        }

        private async Task LoadVehicleTypesAsync()
        {
            ViewBag.ImsConnected = _ims.IsConfigured;
            if (!_ims.IsConfigured)
                return;

            try
            {
                ViewBag.VehicleTypes = await _ims.GetVehicleTypesAsync();
            }
            catch (ImsApiException ex)
            {
                ViewBag.ImsError = ex.Message;
                ViewBag.VehicleTypes = new List<LisaVehicleType>();
            }
        }

        private Dictionary<string, object?> BuildLisaBody(CompulsoryPolicyVM model)
        {
            var days = model.CoverDuration switch
            {
                "six" => 180,
                "three" => 90,
                _ => 364
            };
            var start = DateTime.SpecifyKind(model.StartDate.Date, DateTimeKind.Unspecified);
            var body = new Dictionary<string, object?>
            {
                ["insuredsName"] = model.InsuredName,
                ["nationality"] = model.Nationality,
                ["nidPassport"] = model.NationalId,
                ["phoneNo"] = model.Phone,
                ["address"] = "غير محدد",
                ["fromNoonOf"] = new DateTimeOffset(start, TimeSpan.FromHours(2)).ToString("yyyy-MM-dd'T'HH':'mm':'sszzz", System.Globalization.CultureInfo.InvariantCulture),
                ["typeOfVehicle"] = model.Make,
                ["plateNo"] = model.PlateNumber,
                ["chassisNo"] = model.ChassisNumber,
                ["color"] = model.Color,
                ["yearMade"] = model.ManufactureYear,
                ["loadTon"] = model.LoadTons.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["tonnage"] = model.LoadTons,
                ["regAuthority"] = string.IsNullOrWhiteSpace(model.RegistrationAuthority) ? "غير محدد" : model.RegistrationAuthority,
                ["issuanceCenter"] = _ims.BranchName,
                ["purposeLicense"] = model.Make,
                ["dayOfCarType"] = days,
                ["typeVechicleId"] = model.MainVehicleType,
                ["typeVechicle2Id"] = model.ModelDetail,
                ["issuingFeesOptions"] = 0,
                ["passengersNo"] = model.Passengers,
                ["engineHp"] = model.EngineHp,
                ["issuerName"] = _ims.IssuerName,
                ["issuerType"] = 3
            };

            if (!string.IsNullOrWhiteSpace(model.ExtraDetail) && Guid.TryParse(model.ExtraDetail, out _))
                body["typeVechicle3Id"] = model.ExtraDetail;

            return body;
        }

        /// <summary>
        /// LISA replies with the plain text "Bad Request" and no field name.
        /// These are the inputs that produce that exact reply.
        /// </summary>
        private static string? DescribeLisaRejection(CompulsoryPolicyVM model)
        {
            var notes = new List<string>();
            var libyaNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, LibyaTimeZone());
            var earliest = libyaNow.Hour >= 12 ? libyaNow.Date.AddDays(1) : libyaNow.Date;
            if (model.StartDate.Year < 2000 || model.StartDate.Date < earliest)
                notes.Add($"تاريخ البدء ({model.StartDate:yyyy-MM-dd}) يجب أن يكون {earliest:yyyy-MM-dd} أو بعده. التغطية تبدأ من الظهر، وظهر اليوم قد فات.");
            if (string.IsNullOrWhiteSpace(model.InsuredName))
                notes.Add("اسم المؤمن له فارغ");
            if (string.IsNullOrWhiteSpace(model.PlateNumber))
                notes.Add("اللوحة المعدنية فارغة");
            if (!Guid.TryParse(model.MainVehicleType, out _))
                notes.Add("اختر نوع المركبة الرئيسي من القائمة");
            if (!Guid.TryParse(model.ModelDetail, out _))
                notes.Add("اختر الموديل المحدد من القائمة");

            return notes.Count == 0 ? null : string.Join(" — ", notes);
        }

        private static TimeZoneInfo LibyaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Libya Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("Libya", TimeSpan.FromHours(2), "Libya", "Libya");
            }
        }

        [HttpGet]
        public IActionResult OrangeCard()
        {
            ViewData["Title"] = "وثيقة جديدة - بطاقة عربية موحدة (برتقالية)";
            ViewData["PolicyKind"] = PolicyProductRoutes.OrangeCard;
            return View(new OrangeCardPolicyVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PreviewOrange(OrangeCardPolicyVM model)
            => DemoResult(model, "OrangeCard", "معاينة", null);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IssueOrange(OrangeCardPolicyVM model)
            => DemoResult(model, "OrangeCard", "إصدار", $"DEMO-ORG-{DateTime.Now:yyyyMMddHHmmss}");

        [HttpGet]
        public IActionResult Travelers()
        {
            ViewData["Title"] = "وثيقة جديدة - تأمين مسافرين";
            ViewData["PolicyKind"] = PolicyProductRoutes.Travelers;
            return View(new TravelersPolicyVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PreviewTravel(TravelersPolicyVM model)
            => DemoResult(model, "Travelers", "معاينة", null);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IssueTravel(TravelersPolicyVM model)
            => DemoResult(model, "Travelers", "إصدار", $"DEMO-TRV-{DateTime.Now:yyyyMMddHHmmss}");

        private IActionResult DemoResult(object model, string backAction, string actionLabel, string? policyRef)
        {
            if (!ModelState.IsValid)
                return View(backAction, model);

            ViewBag.DemoMode = true;
            ViewBag.ActionLabel = actionLabel;
            ViewBag.PolicyRef = policyRef;
            ViewBag.BackAction = backAction;
            ViewBag.PolicyKind = backAction;
            return View("Result", model);
        }
    }
}
