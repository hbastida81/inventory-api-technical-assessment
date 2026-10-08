using System.Data;
using Dapper;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Exceptions;
using Inventory.Application.InventoryMovements.Models;
using Inventory.Domain.Enums;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class MovementWriteRepository : IMovementWriteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MovementWriteRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<MovementResult> RegisterEntryAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var now = DateTime.UtcNow;
            await connection.ExecuteAsync(
                "UPDATE Products SET Stock = Stock + @Quantity, UpdatedAt = @Now WHERE Id = @ProductId;",
                new { ProductId = productId, Quantity = quantity, Now = now },
                transaction);
            var movementId = await connection.QuerySingleAsync<int>(
                @"INSERT INTO InventoryMovements (ProductId, Type, Quantity, CreatedAt)
                  VALUES (@ProductId, @Type, @Quantity, @CreatedAt);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { ProductId = productId, Type = (int)MovementType.Entry, Quantity = quantity, CreatedAt = now },
                transaction);
            transaction.Commit();

            return new MovementResult(movementId, productId, "Entry", quantity, now);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<MovementResult> RegisterExitAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var now = DateTime.UtcNow;
            var affected = await connection.ExecuteAsync(
                "UPDATE Products SET Stock = Stock - @Quantity, UpdatedAt = @Now WHERE Id = @ProductId AND Stock >= @Quantity;",
                new { ProductId = productId, Quantity = quantity, Now = now },
                transaction);
            if (affected == 0)
            {
                transaction.Rollback();
                throw new ConflictException("Insufficient stock for the requested exit quantity.");
            }
            var movementId = await connection.QuerySingleAsync<int>(
                @"INSERT INTO InventoryMovements (ProductId, Type, Quantity, CreatedAt)
                  VALUES (@ProductId, @Type, @Quantity, @CreatedAt);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { ProductId = productId, Type = (int)MovementType.Exit, Quantity = quantity, CreatedAt = now },
                transaction);
            transaction.Commit();

            return new MovementResult(movementId, productId, "Exit", quantity, now);
        }
        catch (ConflictException)
        {
            throw;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
