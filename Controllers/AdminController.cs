using Awai.Classes;
using Awai.Models.Entities;
using Awai.Models.Interfaces;
using Awai.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Awai.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Prog,Admin,Employee")]
    public class AdminController : BaseController
    {
        private readonly IUnitOfWork<Product> _products;
        private readonly IUnitOfWork<Post> _posts;
        private readonly IUnitOfWork<InsuranceApplication> _apps;
        private readonly IUnitOfWork<ContactMessage> _messages;
        private readonly IUnitOfWork<SiteInfo> _siteInfo;
        private readonly IUnitOfWork<PageContent> _pages;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            IWebHostEnvironment host,
            IUnitOfWork<Product> products,
            IUnitOfWork<Post> posts,
            IUnitOfWork<InsuranceApplication> apps,
            IUnitOfWork<ContactMessage> messages,
            IUnitOfWork<SiteInfo> siteInfo,
            IUnitOfWork<PageContent> pages,
            UserManager<ApplicationUser> userManager) : base(host)
        {
            _products = products;
            _posts = posts;
            _apps = apps;
            _messages = messages;
            _siteInfo = siteInfo;
            _pages = pages;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("Dashboard")]
        public async Task<IActionResult> Index()
        {
            var apps = _apps.Repository.Include(a => a.Product);
            var vm = new DashboardVM
            {
                ProductsCount = await _products.Repository.GetAll().CountAsync(),
                PostsCount = await _posts.Repository.GetAll().CountAsync(),
                ApplicationsCount = await apps.CountAsync(),
                NewApplications = await _apps.Repository.GetWhere(a => a.Status == ApplicationStatus.Submitted).CountAsync(),
                MessagesCount = await _messages.Repository.GetAll().CountAsync(),
                PendingUsers = await _userManager.Users.CountAsync(u => u.Approval != true),
                RecentApplications = await apps.OrderByDescending(a => a.Created).Take(8).ToListAsync()
            };
            return View(vm);
        }

        [Authorize(Roles = "Prog,Admin")]
        public async Task<IActionResult> Company()
        {
            var site = await _siteInfo.Repository.GetAll(true).FirstOrDefaultAsync();
            if (site == null)
                return View("NotFound");

            var about = await _pages.Repository.GetWhere(p => p.Key == "AboutUs").FirstOrDefaultAsync();
            var who = await _pages.Repository.GetWhere(p => p.Key == "WhoWeAre").FirstOrDefaultAsync();

            return View(new CompanyContentVM
            {
                Site = site,
                AboutId = about?.Id ?? Guid.Empty,
                AboutTitle = about?.Title ?? "من نحن",
                AboutBody = about?.Body ?? "",
                WhoWeAreId = who?.Id ?? Guid.Empty,
                WhoWeAreTitle = who?.Title ?? "من نكون",
                WhoWeAreBody = who?.Body ?? ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Prog,Admin")]
        public async Task<IActionResult> Company(CompanyContentVM model)
        {
            var site = await _siteInfo.Repository.GetByIdAsync(model.Site.Id);
            if (site == null)
                return View("NotFound");

            if (!CheckImgExtension(model.Site.Logo))
            {
                TempData["ErrorMessage"] = "صيغة الشعار غير مدعومة";
                return View(model);
            }

            site.Name = model.Site.Name;
            site.Slogan = model.Site.Slogan;
            site.Phone = model.Site.Phone;
            site.Phone2 = model.Site.Phone2;
            site.WhatsAppNumber = model.Site.WhatsAppNumber;
            site.Email = model.Site.Email;
            site.Address = model.Site.Address;
            site.MapUrl = model.Site.MapUrl;
            site.MapEmbedUrl = model.Site.MapEmbedUrl;
            site.Modified = DateTime.UtcNow;
            site.LogoUrl = UploadFile("logo", model.Site.Logo, site.LogoUrl, model.KeepLogo ?? site.LogoUrl ?? "keep");
            _siteInfo.Repository.Update(site);
            await _siteInfo.SaveAsync();

            await SavePageAsync(model.AboutId, "AboutUs", model.AboutTitle, model.AboutBody);
            await SavePageAsync(model.WhoWeAreId, "WhoWeAre", model.WhoWeAreTitle, model.WhoWeAreBody);

            TempData["SuccessMessage"] = "تم حفظ بيانات الشركة والصفحات";
            return RedirectToAction(nameof(Company));
        }

        private async Task SavePageAsync(Guid id, string key, string title, string body)
        {
            PageContent? page = id != Guid.Empty
                ? await _pages.Repository.GetByIdAsync(id)
                : await _pages.Repository.GetWhere(p => p.Key == key, true).FirstOrDefaultAsync();

            if (page == null)
            {
                _pages.Repository.Insert(new PageContent
                {
                    Key = key,
                    Title = title,
                    Body = body
                });
            }
            else
            {
                page.Title = title;
                page.Body = body;
                page.Modified = DateTime.UtcNow;
                _pages.Repository.Update(page);
            }

            await _pages.SaveAsync();
        }
    }
}
