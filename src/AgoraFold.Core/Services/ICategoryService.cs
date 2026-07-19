using AgoraFold.Core.Entities;

namespace AgoraFold.Core.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
}
