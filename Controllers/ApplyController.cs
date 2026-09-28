using Awai.Helpers;
using Awai.Models.Entities;
using Awai.Models.Interfaces;
using Awai.Services;
using Awai.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Awai.Controllers
{
    public class ApplyController : BaseController
    {
        private readonly IUnitOfWork<Product> _products;
        private readonly IUnitOfWork<SiteInfo> _site;
        private readonly IApplicationSubmissionService _submission;

        public ApplyController(
            IWebHostEnvironment host,
            IUnitOfWork<Product> products,
            IUnitOfWork<SiteInfo> site,
            IApplicationSubmissionService submission) : base(host)
        {
            _products = products;
            _site = site;
            _submission = submission;
        }

        [HttpGet]
        public async Task<IActionResult> Index(Guid? productId)
        {
            await FillProducts(productId);
            await FillSite();

            if (productId.HasValue && productId != Guid.Empty)
            {
                var product = await _products.Repository.GetByIdAsync(productId.Value);
                var action = PolicyProductRoutes.ActionFor(product?.Title);
                if (!string.IsNullOrEmpty(action))
                    return RedirectToAction(action, "Policy");
            }

            return View(new ApplyVM { ProductId = productId ?? Guid.Empty });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Index(ApplyVM model)
        {
            await FillProducts(model.ProductId);
            await FillSite();

            if (model.ProductId == Guid.Empty)
                ModelState.AddModelError(nameof(model.ProductId), "اختر نوع التأمين");

            if (model.IdDocument != null && model.IdDocument.Length > 0 && !CheckImgExtension(model.IdDocument))
                ModelState.AddModelError(nameof(model.IdDocument), "صورة الهوية يجب أن تكون صورة فقط (jpg / png / webp...)");

            if (model.MainDocument == null || model.MainDocument.Length == 0)
                ModelState.AddModelError(nameof(model.MainDocument), "أرفق صورة المستند / فاتورة السداد");
            else if (!CheckImgExtension(model.MainDocument))
                ModelState.AddModelError(nameof(model.MainDocument), "المستند يجب أن يكون صورة فقط");

            if (model.ExtraDocument != null && model.ExtraDocument.Length > 0 && !CheckImgExtension(model.ExtraDocument))
                ModelState.AddModelError(nameof(model.ExtraDocument), "المستند الإضافي يجب أن يكون صورة فقط");

            if (!ModelState.IsValid)
                return View(model);

            var application = new InsuranceApplication
            {
                ProductId = model.ProductId,
                FullName = model.FullName,
                NationalId = model.NationalId,
                DateOfBirth = model.DateOfBirth,
                Phone = model.Phone,
                Email = model.Email,
                Address = model.Address,
                Notes = model.Notes
            };

            if (model.IdDocument is { Length: > 0 })
            {
                application.Documents.Add(new ApplicationDocument
                {
                    DocumentType = "هوية",
                    OriginalFileName = model.IdDocument.FileName,
                    FileUrl = SaveApplicationFile(model.IdDocument)!
                });
            }

            application.Documents.Add(new ApplicationDocument
            {
                DocumentType = "المستند (فاتورة السداد)",
                OriginalFileName = model.MainDocument!.FileName,
                FileUrl = SaveApplicationFile(model.MainDocument)!
            });

            if (model.ExtraDocument is { Length: > 0 })
            {
                application.Documents.Add(new ApplicationDocument
                {
                    DocumentType = "المستند الإضافي",
                    OriginalFileName = model.ExtraDocument.FileName,
                    FileUrl = SaveApplicationFile(model.ExtraDocument)!
                });
            }

            await _submission.SubmitAsync(application);

            var site = ViewBag.Site as SiteInfo;
            var wa = DigitsOnly(site?.WhatsAppNumber ?? site?.Phone ?? "218940001097");
            var msg = Uri.EscapeDataString(
                $"السلام عليكم، قدّمت طلب تأمين عبر الموقع.\nالاسم: {model.FullName}\nالهاتف: {model.Phone}");
            TempData["WhatsAppUrl"] = $"https://wa.me/{wa}?text={msg}";
            TempData["SuccessMessage"] = "تم حفظ طلبك. سنتواصل معك بعد مراجعة المستندات وتحديد السعر.";
            return RedirectToAction(nameof(Thanks));
        }

        public async Task<IActionResult> Thanks()
        {
            await FillSite();
            return View();
        }

        private async Task FillProducts(Guid? selected)
        {
            var list = await _products.Repository.GetWhere(p => p.IsPublished).OrderBy(p => p.SortOrder).ToListAsync();
            ViewBag.Products = new SelectList(list, "Id", "Title", selected);
            ViewBag.PolicyRoutes = list.ToDictionary(
                p => p.Id.ToString(),
                p => PolicyProductRoutes.ActionFor(p.Title) ?? string.Empty);
        }

        private async Task FillSite()
        {
            ViewBag.Site = await _site.Repository.GetAll().FirstOrDefaultAsync();
        }

        private static string DigitsOnly(string value)
            => new string(value.Where(char.IsDigit).ToArray());
    }
}
