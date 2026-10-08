namespace Inventory.Application.Abstractions.Persistence;

public interface IProductWriteRepository
{
    Task<int> CreateAsync(string name, int categoryId, decimal price, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, string name, int categoryId, decimal price, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasMovementsAsync(int id, CancellationToken cancellationToken = default);
}
