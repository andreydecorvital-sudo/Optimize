using Optimize.Core.Models;

namespace Optimize.Core.Services;

public interface ISystemInspectionService
{
    Task<SystemSnapshot> InspectAsync(CancellationToken cancellationToken = default);
}
