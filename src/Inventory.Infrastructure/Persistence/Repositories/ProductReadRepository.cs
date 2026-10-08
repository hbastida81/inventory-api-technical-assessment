using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class ProductReadRepository : IProductReadRepository
{
    private readonly InventoryDbContext _context;

    public ProductReadRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<ProductResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Join(_context.Categories, p => p.CategoryId, c => c.Id, (p, c) => new ProductResult(
                p.Id, p.Name, p.CategoryId, c.Name, p.Price, p.Stock, p.CreatedAt, p.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductResult>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Join(_context.Categories, p => p.CategoryId, c => c.Id, (p, c) => new ProductResult(
                p.Id, p.Name, p.CategoryId, c.Name, p.Price, p.Stock, p.CreatedAt, p.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
