using Awai.Models.Entities;

namespace Awai.Services
{
    /// <summary>
    /// Saves applications locally. Replace with an HTTP implementation when the insurer API is available.
    /// </summary>
    public interface IApplicationSubmissionService
    {
        Task SubmitAsync(InsuranceApplication application, CancellationToken cancellationToken = default);
    }
}
