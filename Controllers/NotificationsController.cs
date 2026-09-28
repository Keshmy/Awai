using Awai.Classes;
using Awai.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Awai.Controllers
{
    [ViewLayout("_LayoutDashboard")]
    [Authorize(Roles = "Prog,Admin,Employee")]
    [Route("Notifications")]
    public class NotificationsController : Controller
    {
        private readonly IStaffNotificationService _notifications;

        public NotificationsController(IStaffNotificationService notifications)
        {
            _notifications = notifications;
        }

        [HttpGet("Active")]
        public async Task<IActionResult> Active(CancellationToken cancellationToken)
        {
            var items = await _notifications.GetActiveAsync(cancellationToken);
            return Json(items);
        }

        [HttpPost("Dismiss/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dismiss(Guid id, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ok = await _notifications.DismissAsync(id, userId, cancellationToken);
            if (!ok)
                return NotFound();

            return Ok(new { id });
        }
    }
}
