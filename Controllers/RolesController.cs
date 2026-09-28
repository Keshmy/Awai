using Awai.Classes;
using Awai.ViewModels.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Awai.Controllers
{
    [Authorize(Roles = "Prog,Admin")]
    [ViewLayout("_LayoutDashboard")]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<Awai.Models.Entities.ApplicationUser> _userManager;

        public RolesController(RoleManager<IdentityRole> roleManager, UserManager<Awai.Models.Entities.ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            var list = new List<RoleVM>();
            foreach (var role in roles)
            {
                list.Add(new RoleVM
                {
                    Id = role.Id,
                    Name = role.Name ?? "",
                    UsersCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count
                });
            }
            return View(list);
        }

        [Authorize(Roles = "Prog")]
        public IActionResult Create() => View(new CreateRoleVM());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Prog")]
        public async Task<IActionResult> Create(CreateRoleVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (string.Equals(model.Name, "Prog", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Name), "صلاحية المبرمج محجوزة ولا يمكن إنشاؤها.");
                return View(model);
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(model.Name));
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "تم إنشاء الصلاحية";
                return RedirectToAction(nameof(Index));
            }
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return View(model);
        }
    }
}
