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
    public class MessagesController : Controller
    {
        private readonly IUnitOfWork<ContactMessage> _messages;

        public MessagesController(IUnitOfWork<ContactMessage> messages)
        {
            _messages = messages;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _messages.Repository.GetAll().OrderByDescending(m => m.Created).ToListAsync());
        }
    }
}
