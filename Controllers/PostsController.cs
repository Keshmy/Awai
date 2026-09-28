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
    public class PostsController : BaseController
    {
        private readonly IUnitOfWork<Post> _posts;

        public PostsController(IWebHostEnvironment host, IUnitOfWork<Post> posts) : base(host)
        {
            _posts = posts;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _posts.Repository.GetAll().OrderByDescending(p => p.PublishedAt).ToListAsync());
        }

        public IActionResult Create() => View(new Post());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post model, string? isImg)
        {
            if (!CheckImgExtension(model.Image))
                ModelState.AddModelError(nameof(model.Image), "صيغة الصورة غير مدعومة");
            if (!ModelState.IsValid)
                return View(model);

            model.ImageUrl = UploadFile("posts", model.Image, null, isImg ?? (model.Image != null ? "1" : null));
            _posts.Repository.Insert(model);
            await _posts.SaveAsync();
            TempData["SuccessMessage"] = "تم نشر العرض";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var item = await _posts.Repository.GetByIdAsync(id);
            return item == null ? View("NotFound") : View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Post model, string? isImg)
        {
            if (!CheckImgExtension(model.Image))
                ModelState.AddModelError(nameof(model.Image), "صيغة الصورة غير مدعومة");
            if (!ModelState.IsValid)
                return View(model);

            model.ImageUrl = UploadFile("posts", model.Image, model.ImageUrl, isImg ?? model.ImageUrl ?? (model.Image != null ? "1" : null));
            model.Modified = DateTime.UtcNow;
            _posts.Repository.Update(model);
            await _posts.SaveAsync();
            TempData["SuccessMessage"] = "تم التحديث";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var item = await _posts.Repository.GetByIdAsync(id);
            if (item == null)
                return View("NotFound");
            DeleteOldFile(item.ImageUrl);
            _posts.Repository.Delete(item);
            await _posts.SaveAsync();
            TempData["SuccessMessage"] = "تم الحذف";
            return RedirectToAction(nameof(Index));
        }
    }
}
