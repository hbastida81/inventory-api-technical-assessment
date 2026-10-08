namespace Inventory.Application.Abstractions.Persistence;

public interface ICategoryWriteRepository
{
    Task<int> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, string name, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasProductsAsync(int id, CancellationToken cancellationToken = default);
}
