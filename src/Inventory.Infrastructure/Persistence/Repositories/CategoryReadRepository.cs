using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Categories.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class CategoryReadRepository : ICategoryReadRepository
{
    private readonly InventoryDbContext _context;

    public CategoryReadRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryResult(c.Id, c.Name, c.CreatedAt, c.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryResult>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Select(c => new CategoryResult(c.Id, c.Name, c.CreatedAt, c.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
