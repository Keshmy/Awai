using Awai.Classes;
using Awai.Models.Entities;
using Awai.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Awai.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Prog,Admin")]
    public class PhotosController : BaseController
    {
        private readonly IUnitOfWork<SitePhoto> _photos;

        public PhotosController(IWebHostEnvironment host, IUnitOfWork<SitePhoto> photos) : base(host)
        {
            _photos = photos;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _photos.Repository.GetAll().OrderBy(p => p.SortOrder).ToListAsync());
        }

        public IActionResult Create() => View(new SitePhoto());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SitePhoto model)
        {
            if (model.Image == null || !CheckImgExtension(model.Image))
                ModelState.AddModelError(nameof(model.Image), "أرفق صورة صالحة");
            if (!ModelState.IsValid)
                return View(model);

            model.ImageUrl = UploadFile("gallery", model.Image, null, "1") ?? "";
            _photos.Repository.Insert(model);
            await _photos.SaveAsync();
            TempData["SuccessMessage"] = "تمت إضافة الصورة";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var item = await _photos.Repository.GetByIdAsync(id);
            return item == null ? View("NotFound") : View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SitePhoto model, string? isImg)
        {
            if (!CheckImgExtension(model.Image))
                ModelState.AddModelError(nameof(model.Image), "صيغة الصورة غير مدعومة");
            if (!ModelState.IsValid)
                return View(model);

            model.ImageUrl = UploadFile("gallery", model.Image, model.ImageUrl, isImg ?? model.ImageUrl)!;
            model.Modified = DateTime.UtcNow;
            _photos.Repository.Update(model);
            await _photos.SaveAsync();
            TempData["SuccessMessage"] = "تم التحديث";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var item = await _photos.Repository.GetByIdAsync(id);
            if (item == null)
                return View("NotFound");
            DeleteOldFile(item.ImageUrl);
            _photos.Repository.Delete(item);
            await _photos.SaveAsync();
            TempData["SuccessMessage"] = "تم الحذف";
            return RedirectToAction(nameof(Index));
        }
    }
}
