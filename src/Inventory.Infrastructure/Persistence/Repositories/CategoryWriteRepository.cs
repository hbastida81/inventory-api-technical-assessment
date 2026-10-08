using Dapper;
using Inventory.Application.Abstractions.Persistence;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class CategoryWriteRepository : ICategoryWriteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CategoryWriteRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO Categories (Name, CreatedAt, UpdatedAt)
            VALUES (@Name, @Now, @Now);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await connection.QuerySingleAsync<int>(sql, new { Name = name, Now = DateTime.UtcNow });
    }

    public async Task<bool> UpdateAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE Categories
            SET Name = @Name, UpdatedAt = @Now
            WHERE Id = @Id;";
        var affected = await connection.ExecuteAsync(sql, new { Id = id, Name = name, Now = DateTime.UtcNow });

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync("DELETE FROM Categories WHERE Id = @Id;", new { Id = id });

        return affected > 0;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleAsync<bool>("SELECT CASE WHEN EXISTS(SELECT 1 FROM Categories WHERE Id = @Id) THEN 1 ELSE 0 END;", new { Id = id });
    }

    public async Task<bool> HasProductsAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleAsync<bool>("SELECT CASE WHEN EXISTS(SELECT 1 FROM Products WHERE CategoryId = @Id) THEN 1 ELSE 0 END;", new { Id = id });
    }
}
