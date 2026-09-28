using Awai.Models;
using Awai.Models.Entities;
using Awai.Models.Interfaces;
using Awai.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Awai.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUnitOfWork<Product> _products;
        private readonly IUnitOfWork<Post> _posts;
        private readonly IUnitOfWork<SitePhoto> _photos;
        private readonly IUnitOfWork<SiteInfo> _site;
        private readonly IUnitOfWork<PageContent> _pages;
        private readonly IUnitOfWork<ContactMessage> _messages;

        public HomeController(
            IUnitOfWork<Product> products,
            IUnitOfWork<Post> posts,
            IUnitOfWork<SitePhoto> photos,
            IUnitOfWork<SiteInfo> site,
            IUnitOfWork<PageContent> pages,
            IUnitOfWork<ContactMessage> messages)
        {
            _products = products;
            _posts = posts;
            _photos = photos;
            _site = site;
            _pages = pages;
            _messages = messages;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new HomeVM
            {
                Site = await _site.Repository.GetAll().FirstOrDefaultAsync(),
                Posts = await _posts.Repository.GetWhere(p => p.IsPublished).OrderByDescending(p => p.PublishedAt).Take(6).ToListAsync(),
                Photos = await _photos.Repository.GetWhere(p => p.IsPublished).OrderBy(p => p.SortOrder).ToListAsync(),
                About = await _pages.Repository.GetWhere(p => p.Key == "AboutUs").FirstOrDefaultAsync()
            };
            return View(vm);
        }

        public async Task<IActionResult> About()
        {
            var page = await _pages.Repository.GetWhere(p => p.Key == "AboutUs").FirstOrDefaultAsync();
            ViewData["Title"] = page?.Title ?? "من نحن";
            return View("CmsPage", page);
        }

        public async Task<IActionResult> WhoWeAre()
        {
            var page = await _pages.Repository.GetWhere(p => p.Key == "WhoWeAre").FirstOrDefaultAsync();
            ViewData["Title"] = page?.Title ?? "من نكون";
            return View("CmsPage", page);
        }

        public async Task<IActionResult> Product(Guid id)
        {
            var product = await _products.Repository.GetByIdAsync(id);
            if (product == null || !product.IsPublished)
                return View("NotFound");
            return View(product);
        }

        public async Task<IActionResult> Post(Guid id)
        {
            var post = await _posts.Repository.GetByIdAsync(id);
            if (post == null || !post.IsPublished)
                return View("NotFound");
            return View(post);
        }

        [HttpGet]
        public async Task<IActionResult> News(int page = 1)
        {
            const int pageSize = 9;
            page = Math.Max(1, page);

            var query = _posts.Repository
                .GetWhere(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedAt);

            var total = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            if (page > totalPages)
                page = totalPages;

            var posts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(new NewsListVM
            {
                Posts = posts,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            });
        }

        [HttpGet]
        public async Task<IActionResult> Contact()
        {
            ViewBag.Site = await _site.Repository.GetAll().FirstOrDefaultAsync();
            return View(new ContactVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactVM model)
        {
            ViewBag.Site = await _site.Repository.GetAll().FirstOrDefaultAsync();
            if (!ModelState.IsValid)
                return View(model);

            _messages.Repository.Insert(new ContactMessage
            {
                Name = model.Name,
                Phone = model.Phone,
                Email = model.Email,
                Message = model.Message
            });
            await _messages.SaveAsync();
            TempData["SuccessMessage"] = "تم إرسال رسالتك وسنتواصل معك قريباً";
            return RedirectToAction(nameof(Contact));
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
