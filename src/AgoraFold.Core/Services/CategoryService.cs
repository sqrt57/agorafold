using AgoraFold.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgoraFold.Core.Services;

public sealed class CategoryService(AppDbContext db) : ICategoryService
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
}
