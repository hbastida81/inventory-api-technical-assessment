using Dapper;
using Inventory.Application.Abstractions.Persistence;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class ProductWriteRepository : IProductWriteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProductWriteRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(string name, int categoryId, decimal price, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO Products (Name, CategoryId, Price, Stock, CreatedAt, UpdatedAt)
            VALUES (@Name, @CategoryId, @Price, 0, @Now, @Now);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await connection.QuerySingleAsync<int>(sql, new { Name = name, CategoryId = categoryId, Price = price, Now = DateTime.UtcNow });
    }

    public async Task<bool> UpdateAsync(int id, string name, int categoryId, decimal price, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE Products
            SET Name = @Name, CategoryId = @CategoryId, Price = @Price, UpdatedAt = @Now
            WHERE Id = @Id;";
        var affected = await connection.ExecuteAsync(sql, new { Id = id, Name = name, CategoryId = categoryId, Price = price, Now = DateTime.UtcNow });

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync("DELETE FROM Products WHERE Id = @Id;", new { Id = id });

        return affected > 0;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleAsync<bool>("SELECT CASE WHEN EXISTS(SELECT 1 FROM Products WHERE Id = @Id) THEN 1 ELSE 0 END;", new { Id = id });
    }

    public async Task<bool> HasMovementsAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleAsync<bool>("SELECT CASE WHEN EXISTS(SELECT 1 FROM InventoryMovements WHERE ProductId = @Id) THEN 1 ELSE 0 END;", new { Id = id });
    }
}
