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
    public class ProductsController : Controller
    {
        private readonly IUnitOfWork<Product> _products;

        public ProductsController(IUnitOfWork<Product> products)
        {
            _products = products;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _products.Repository.GetAll().OrderBy(p => p.SortOrder).ToListAsync());
        }

        public IActionResult Create() => View(new Product());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model)
        {
            if (!ModelState.IsValid)
                return View(model);
            _products.Repository.Insert(model);
            await _products.SaveAsync();
            TempData["SuccessMessage"] = "تمت إضافة نوع التأمين";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var item = await _products.Repository.GetByIdAsync(id);
            return item == null ? View("NotFound") : View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product model)
        {
            if (!ModelState.IsValid)
                return View(model);
            model.Modified = DateTime.UtcNow;
            _products.Repository.Update(model);
            await _products.SaveAsync();
            TempData["SuccessMessage"] = "تم التحديث";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var item = await _products.Repository.GetByIdAsync(id);
            if (item == null)
                return View("NotFound");
            _products.Repository.Delete(item);
            await _products.SaveAsync();
            TempData["SuccessMessage"] = "تم الحذف";
            return RedirectToAction(nameof(Index));
        }
    }
}
