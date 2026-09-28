using Awai.Hubs;
using Awai.Models.Entities;
using Awai.Models.Interfaces;
using Awai.ViewModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Awai.Services
{
    public class StaffNotificationService : IStaffNotificationService
    {
        private readonly IUnitOfWork<StaffNotification> _notifications;
        private readonly IUnitOfWork<Product> _products;
        private readonly IHubContext<NotificationHub> _hub;

        public StaffNotificationService(
            IUnitOfWork<StaffNotification> notifications,
            IUnitOfWork<Product> products,
            IHubContext<NotificationHub> hub)
        {
            _notifications = notifications;
            _products = products;
            _hub = hub;
        }

        public async Task NotifyNewApplicationAsync(InsuranceApplication application, CancellationToken cancellationToken = default)
        {
            var productTitle = application.Product?.Title;
            if (string.IsNullOrWhiteSpace(productTitle) && application.ProductId != Guid.Empty)
            {
                var product = await _products.Repository.GetByIdAsync(application.ProductId);
                productTitle = product?.Title;
            }

            productTitle ??= "تأمين";

            var notification = new StaffNotification
            {
                ApplicationId = application.Id,
                Title = "طلب تأمين جديد",
                Message = $"{application.FullName} — {productTitle} — {application.Phone}"
            };

            _notifications.Repository.Insert(notification);
            await _notifications.SaveAsync();

            var payload = ToVm(notification);
            await _hub.Clients.Group(NotificationHub.StaffGroup)
                .SendAsync("ReceiveNotification", payload, cancellationToken);
        }

        public async Task<bool> DismissAsync(Guid id, string? userId, CancellationToken cancellationToken = default)
        {
            var notification = await _notifications.Repository.GetByIdAsync(id);
            if (notification == null)
                return false;

            if (!notification.IsDismissed)
            {
                notification.IsDismissed = true;
                notification.DismissedAt = DateTime.UtcNow;
                notification.DismissedByUserId = userId;
                notification.Modified = DateTime.UtcNow;
                _notifications.Repository.Update(notification);
                await _notifications.SaveAsync();
            }

            await _hub.Clients.Group(NotificationHub.StaffGroup)
                .SendAsync("NotificationDismissed", id, cancellationToken);

            return true;
        }

        public async Task<List<StaffNotificationItemVM>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            var list = await _notifications.Repository
                .GetWhere(n => !n.IsDismissed)
                .OrderByDescending(n => n.Created)
                .Take(50)
                .ToListAsync(cancellationToken);

            return list.Select(ToVm).ToList();
        }

        private static StaffNotificationItemVM ToVm(StaffNotification n) => new()
        {
            Id = n.Id,
            ApplicationId = n.ApplicationId,
            Title = n.Title,
            Message = n.Message,
            CreatedLocal = n.CreatedDateLocalTime
        };
    }
}
