using Awai.Classes;
using Awai.Models.Entities;
using Awai.Models.Interfaces;
using Awai.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Awai.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Prog,Admin,Employee")]
    public class ReportsController : Controller
    {
        private readonly IUnitOfWork<InsuranceApplication> _apps;
        private readonly IUnitOfWork<Product> _products;

        public ReportsController(IUnitOfWork<InsuranceApplication> apps, IUnitOfWork<Product> products)
        {
            _apps = apps;
            _products = products;
        }

        [HttpGet]
        public async Task<IActionResult> Index(ReportFilterVM filter)
        {
            filter.FromDate ??= DateTime.Today.AddMonths(-1);
            filter.ToDate ??= DateTime.Today;

            await FillProducts(filter.ProductId);
            filter.Results = await QueryAsync(filter);
            return View(filter);
        }

        private async Task<List<InsuranceApplication>> QueryAsync(ReportFilterVM filter)
        {
            // Created is stored in UTC; convert local day bounds to UTC for filtering.
            var fromUtc = filter.FromDate.HasValue
                ? DateTime.SpecifyKind(filter.FromDate.Value.Date, DateTimeKind.Local).ToUniversalTime()
                : DateTime.MinValue;

            var toUtcExclusive = filter.ToDate.HasValue
                ? DateTime.SpecifyKind(filter.ToDate.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime()
                : DateTime.MaxValue;

            var query = _apps.Repository.Include(a => a.Product)
                .Where(a => a.Created >= fromUtc && a.Created < toUtcExclusive);

            if (filter.ProductId.HasValue && filter.ProductId != Guid.Empty)
                query = query.Where(a => a.ProductId == filter.ProductId);

            if (filter.Status.HasValue)
                query = query.Where(a => a.Status == filter.Status);

            return await query.OrderByDescending(a => a.Created).ToListAsync();
        }

        private async Task FillProducts(Guid? selected)
        {
            var list = await _products.Repository.GetAll().OrderBy(p => p.SortOrder).ToListAsync();
            ViewBag.Products = new SelectList(list, "Id", "Title", selected);
        }
    }
}
