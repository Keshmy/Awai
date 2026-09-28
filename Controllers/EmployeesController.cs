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
    [Authorize(Roles = "Prog,Admin")]
    [ViewLayout("_LayoutDashboard")]
    public class EmployeesController : Controller
    {
        private readonly IUnitOfWork<Employee> _employee;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeesController(IUnitOfWork<Employee> employee, UserManager<ApplicationUser> userManager)
        {
            _employee = employee;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _employee.Repository.GetAll().Include(e => e.ApplicationUser).OrderByDescending(e => e.Created).ToListAsync();
            return View(list);
        }

        public IActionResult Create() => View(new CreateEmployeeVM());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

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
            await _userManager.AddToRoleAsync(user, "Employee");
            model.Employee.UserId = user.Id;
            _employee.Repository.Insert(model.Employee);
            await _employee.SaveAsync();
            TempData["SuccessMessage"] = "تم إنشاء الموظف";
            return RedirectToAction(nameof(Index));
        }
    }
}
