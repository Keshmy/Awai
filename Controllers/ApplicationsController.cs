using Awai.Classes;
using Awai.Helpers;
using Awai.Models.Entities;
using Awai.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Awai.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Prog,Admin,Employee")]
    public class ApplicationsController : Controller
    {
        private readonly IUnitOfWork<InsuranceApplication> _apps;
        private readonly IUnitOfWork<ApplicationDocument> _documents;
        private readonly IWebHostEnvironment _host;

        public ApplicationsController(
            IUnitOfWork<InsuranceApplication> apps,
            IUnitOfWork<ApplicationDocument> documents,
            IWebHostEnvironment host)
        {
            _apps = apps;
            _documents = documents;
            _host = host;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var all = await _apps.Repository.Include(a => a.Product)
                .OrderByDescending(a => a.Created)
                .ToListAsync();

            ViewBag.Status = status;
            ViewBag.Counts = new Dictionary<string, int>
            {
                ["all"] = all.Count,
                ["Submitted"] = all.Count(a => a.Status == ApplicationStatus.Submitted),
                ["Reviewed"] = all.Count(a => a.Status == ApplicationStatus.Reviewed),
                ["Synced"] = all.Count(a => a.Status == ApplicationStatus.Synced),
                ["Failed"] = all.Count(a => a.Status == ApplicationStatus.Failed)
            };

            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<ApplicationStatus>(status, true, out var parsed))
            {
                return View(all.Where(a => a.Status == parsed).ToList());
            }

            return View(all);
        }

        public async Task<IActionResult> Document(Guid id)
        {
            var document = await _documents.Repository.GetWhere(d => d.Id == id).FirstOrDefaultAsync();
            if (document == null)
                return NotFound();

            var path = BaseController.ResolveApplicationFile(_host, document.FileUrl);
            if (path == null)
                return NotFound();

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
                contentType = "application/octet-stream";

            return PhysicalFile(path, contentType);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var app = await _apps.Repository.Include(a => a.Product, a => a.Documents)
                .FirstOrDefaultAsync(a => a.Id == id);
            return app == null ? View("NotFound") : View(app);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(Guid id, ApplicationStatus status)
        {
            var app = await _apps.Repository.GetByIdAsync(id);
            if (app == null)
                return View("NotFound");

            app.Status = status;
            app.Modified = DateTime.UtcNow;
            _apps.Repository.Update(app);
            await _apps.SaveAsync();
            TempData["SuccessMessage"] = $"تم تحديث الحالة إلى: {ApplicationStatusText.Arabic(status)}";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReviewed(Guid id)
            => await SetStatus(id, ApplicationStatus.Reviewed);
    }
}
