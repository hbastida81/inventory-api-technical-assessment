using Inventory.Application.Categories.Models;

namespace Inventory.Application.Abstractions.Persistence;

public interface ICategoryReadRepository
{
    Task<CategoryResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryResult>> GetAllAsync(CancellationToken cancellationToken = default);
}
