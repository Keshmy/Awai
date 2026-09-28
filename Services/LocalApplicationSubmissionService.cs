using Awai.Models.Entities;
using Awai.Models.Interfaces;

namespace Awai.Services
{
    public class LocalApplicationSubmissionService : IApplicationSubmissionService
    {
        private readonly IUnitOfWork<InsuranceApplication> _applications;
        private readonly IStaffNotificationService _notifications;

        public LocalApplicationSubmissionService(
            IUnitOfWork<InsuranceApplication> applications,
            IStaffNotificationService notifications)
        {
            _applications = applications;
            _notifications = notifications;
        }

        public async Task SubmitAsync(InsuranceApplication application, CancellationToken cancellationToken = default)
        {
            application.Status = ApplicationStatus.Submitted;
            _applications.Repository.Insert(application);
            await _applications.SaveAsync();
            await _notifications.NotifyNewApplicationAsync(application, cancellationToken);
        }
    }
}
