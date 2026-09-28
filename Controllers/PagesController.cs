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
    public class PagesController : Controller
    {
        private readonly IUnitOfWork<PageContent> _pages;

        public PagesController(IUnitOfWork<PageContent> pages)
        {
            _pages = pages;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _pages.Repository.GetAll().OrderBy(p => p.Title).ToListAsync());
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var page = await _pages.Repository.GetByIdAsync(id);
            return page == null ? View("NotFound") : View(page);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PageContent model)
        {
            var page = await _pages.Repository.GetByIdAsync(model.Id);
            if (page == null)
                return View("NotFound");

            page.Title = model.Title;
            page.Body = model.Body;
            page.Modified = DateTime.UtcNow;
            _pages.Repository.Update(page);
            await _pages.SaveAsync();
            TempData["SuccessMessage"] = "تم حفظ الصفحة";
            return RedirectToAction(nameof(Index));
        }
    }
}
