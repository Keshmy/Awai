using Awai.Models.Entities;
using Awai.Models.Interfaces;
using Awai.ViewModels.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Awai.Controllers
{
    public class AccountController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork<UserProfile> _userProfile;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IUnitOfWork<UserProfile> userProfile,
            IWebHostEnvironment host) : base(host)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _userProfile = userProfile;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
                return await RedirectAfterLogin();
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(loginVM);

            var user = await _userManager.FindByEmailAsync(loginVM.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "البريد الإلكتروني غير موجود");
                return View(loginVM);
            }

            if (user.Approval != true)
            {
                ModelState.AddModelError(string.Empty, "حسابك بانتظار موافقة الإدارة. لن تتمكن من الدخول إلى لوحة التحكم حتى يتم الاعتماد.");
                return View(loginVM);
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password, loginVM.RememberMe, true);
            if (result.Succeeded)
            {
                user.LastAccessTime = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return await RedirectAfterLogin(user);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "الحساب موقف مؤقتاً بعد محاولات خاطئة.");
                return View(loginVM);
            }

            ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة");
            return View(loginVM);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.Phone,
                EmailConfirmed = true,
                Age = 18,
                Approval = false,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync("Pending"))
                await _roleManager.CreateAsync(new IdentityRole("Pending"));
            await _userManager.AddToRoleAsync(user, "Pending");

            _userProfile.Repository.Insert(new UserProfile
            {
                DisplayName = model.DisplayName,
                UserId = user.Id
            });
            await _userProfile.SaveAsync();

            TempData["SuccessMessage"] = "تم إنشاء الحساب. بعد موافقة الإدارة يمكنك تسجيل الدخول إلى لوحة التحكم.";
            return RedirectToAction(nameof(Login));
        }

        private async Task<IActionResult> RedirectAfterLogin(ApplicationUser? user = null)
        {
            user ??= await _userManager.GetUserAsync(User);
            if (user == null || user.Approval != true)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction(nameof(Login));
            }

            if (await _userManager.IsInRoleAsync(user, "Prog")
                || await _userManager.IsInRoleAsync(user, "Admin")
                || await _userManager.IsInRoleAsync(user, "Employee"))
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> UserProfile(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return View("NotFound");

            ViewBag.Email = user.Email;
            var userProfile = await _userProfile.Repository.GetWhere(p => p.UserId == userId).FirstOrDefaultAsync()
                ?? new UserProfile { UserId = userId };
            return View(userProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UserProfile(UserProfile userProfile, string? isImg1)
        {
            if (!CheckImgExtension(userProfile.Image))
            {
                TempData["ErrorMessage"] = "صيغة الصورة غير مدعومة";
                return View(userProfile);
            }

            userProfile.ImageUrl = UploadFile("UsersProfile", userProfile.Image, userProfile.ImageUrl, isImg1 ?? "1");

            if (userProfile.Id == Guid.Empty)
                _userProfile.Repository.Insert(userProfile);
            else
            {
                userProfile.Modified = DateTime.UtcNow;
                _userProfile.Repository.Update(userProfile);
            }
            await _userProfile.SaveAsync();
            TempData["SuccessMessage"] = "تم الحفظ بنجاح";
            return RedirectToAction("UserProfile", new { userId = userProfile.UserId });
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return View("NotFound");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "تم تغيير كلمة المرور";
                return RedirectToAction("Index", "Admin");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        public IActionResult AccessDenied() => View();
    }
}
