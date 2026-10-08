using Inventory.Application.Products.Models;

namespace Inventory.Application.Abstractions.Persistence;

public interface IProductReadRepository
{
    Task<ProductResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductResult>> GetAllAsync(CancellationToken cancellationToken = default);
}
