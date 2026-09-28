using Awai.Models.Entities;
using Awai.ViewModels;

namespace Awai.Services
{
    public interface IStaffNotificationService
    {
        Task NotifyNewApplicationAsync(InsuranceApplication application, CancellationToken cancellationToken = default);
        Task<bool> DismissAsync(Guid id, string? userId, CancellationToken cancellationToken = default);
        Task<List<StaffNotificationItemVM>> GetActiveAsync(CancellationToken cancellationToken = default);
    }
}
