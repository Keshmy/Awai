using Awai.Classes;
using Awai.Helpers;
using Awai.Models.Entities;
using Awai.ViewModels.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Awai.Controllers
{
    [Authorize(Roles = "Prog,Admin")]
    [ViewLayout("_LayoutDashboard")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;

        // Prog is never assignable — only the seeded programmer keeps it.
        private static readonly string[] AssignableRoles = ["Admin", "Employee", "Customer", "Pending"];

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
        }

        public async Task<IActionResult> Index(string? filter)
        {
            var users = await _userManager.Users.Include(u => u.UserProfile)
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();

            var list = new List<UserListItemVM>();
            foreach (var u in users)
            {
                var roles = (await _userManager.GetRolesAsync(u)).ToList();
                list.Add(new UserListItemVM
                {
                    Id = u.Id,
                    Email = u.Email ?? "",
                    DisplayName = u.UserProfile?.DisplayName,
                    IsApproved = u.Approval == true,
                    Roles = roles,
                    CreatedDate = u.CreatedDate,
                    IsProtected = await ProgrammerGuard.IsProtectedAsync(_userManager, u, _config)
                });
            }

            if (filter == "pending")
                list = list.Where(u => !u.IsApproved && !u.IsProtected).ToList();

            ViewBag.Filter = filter;
            return View(list);
        }

        public IActionResult Create()
        {
            ViewBag.AssignableRoles = GetRolesCurrentUserMayAssign()
                .Where(r => r is "Admin" or "Employee")
                .ToArray();
            return View(new CreateUserVM { Role = "Employee" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVM model)
        {
            ViewBag.AssignableRoles = GetRolesCurrentUserMayAssign()
                .Where(r => r is "Admin" or "Employee")
                .ToArray();

            if (!ModelState.IsValid)
                return View(model);

            if (model.Role == "Prog" || !GetRolesCurrentUserMayAssign().Contains(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "لا يمكنك منح هذه الصلاحية.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Age = model.Age,
                EmailConfirmed = true,
                Approval = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);
                return View(model);
            }
            await EnsureRoleExists(model.Role);
            await _userManager.AddToRoleAsync(user, model.Role);
            TempData["SuccessMessage"] = "تم إنشاء المستخدم";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return View("NotFound");

            if (await ProgrammerGuard.IsProtectedAsync(_userManager, user, _config))
            {
                TempData["ErrorMessage"] = "حساب المبرمج محمي ولا يمكن تعديله.";
                return RedirectToAction(nameof(Index));
            }

            var roles = (await _userManager.GetRolesAsync(user)).ToList();
            return View(new EditUserVM
            {
                Id = user.Id,
                Email = user.Email ?? "",
                Age = user.Age,
                DisplayName = user.UserProfile?.DisplayName,
                IsApproved = user.Approval == true,
                Roles = roles,
                SelectedRoles = roles.Where(r => r != "Prog").ToList(),
                AvailableRoles = GetRolesCurrentUserMayAssign()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserVM model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return View("NotFound");

            if (await ProgrammerGuard.IsProtectedAsync(_userManager, user, _config))
            {
                TempData["ErrorMessage"] = "حساب المبرمج محمي ولا يمكن تعديله.";
                return RedirectToAction(nameof(Index));
            }

            model.SelectedRoles ??= [];
            model.SelectedRoles = model.SelectedRoles.Where(r => r != "Prog").ToList();

            var allowed = GetRolesCurrentUserMayAssign();
            if (model.SelectedRoles.Any(r => !allowed.Contains(r)))
            {
                TempData["ErrorMessage"] = "ليس لديك صلاحية لمنح إحدى الصلاحيات المختارة.";
                return RedirectToAction(nameof(Edit), new { id = model.Id });
            }

            user.Email = model.Email;
            user.UserName = model.Email;
            user.Age = model.Age;
            user.Approval = model.IsApproved;
            user.ModifiedDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var current = (await _userManager.GetRolesAsync(user)).Where(r => r != "Prog").ToList();
            var toRemove = current.Except(model.SelectedRoles).ToList();
            var toAdd = model.SelectedRoles.Except(current).ToList();

            if (toRemove.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, toRemove);

            foreach (var role in toAdd)
            {
                await EnsureRoleExists(role);
                await _userManager.AddToRoleAsync(user, role);
            }

            if (model.IsApproved)
            {
                var rolesNow = await _userManager.GetRolesAsync(user);
                if (!rolesNow.Any(r => r is "Admin" or "Employee" or "Prog"))
                {
                    await EnsureRoleExists("Employee");
                    await _userManager.AddToRoleAsync(user, "Employee");
                }
                if (rolesNow.Contains("Pending"))
                    await _userManager.RemoveFromRoleAsync(user, "Pending");
            }

            TempData["SuccessMessage"] = "تم تحديث المستخدم والصلاحيات";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id, string role = "Employee")
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return View("NotFound");

            if (await ProgrammerGuard.IsProtectedAsync(_userManager, user, _config))
            {
                TempData["ErrorMessage"] = "حساب المبرمج محمي.";
                return RedirectToAction(nameof(Index));
            }

            if (role == "Prog" || !AssignableRoles.Contains(role))
                role = "Employee";

            if (role == "Admin" && !User.IsInRole("Prog"))
                role = "Employee";

            user.Approval = true;
            user.ModifiedDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            if (await _userManager.IsInRoleAsync(user, "Pending"))
                await _userManager.RemoveFromRoleAsync(user, "Pending");

            await EnsureRoleExists(role);
            if (!await _userManager.IsInRoleAsync(user, role))
                await _userManager.AddToRoleAsync(user, role);

            TempData["SuccessMessage"] = $"تم اعتماد الحساب ومنح صلاحية {role}";
            return RedirectToAction(nameof(Index), new { filter = "pending" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return View("NotFound");

            if (await ProgrammerGuard.IsProtectedAsync(_userManager, user, _config))
            {
                TempData["ErrorMessage"] = "حساب المبرمج محمي ولا يمكن إيقافه.";
                return RedirectToAction(nameof(Index));
            }

            user.Approval = false;
            user.ModifiedDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var staffRoles = (await _userManager.GetRolesAsync(user))
                .Where(r => r is "Admin" or "Employee")
                .ToList();
            if (staffRoles.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, staffRoles);

            await EnsureRoleExists("Pending");
            if (!await _userManager.IsInRoleAsync(user, "Pending"))
                await _userManager.AddToRoleAsync(user, "Pending");

            TempData["SuccessMessage"] = "تم إيقاف اعتماد الحساب";
            return RedirectToAction(nameof(Index));
        }

        private List<string> GetRolesCurrentUserMayAssign()
        {
            if (User.IsInRole("Prog"))
                return AssignableRoles.ToList();

            // Admin can manage Employee / Customer / Pending — not Admin or Prog
            return ["Employee", "Customer", "Pending"];
        }

        private async Task EnsureRoleExists(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
