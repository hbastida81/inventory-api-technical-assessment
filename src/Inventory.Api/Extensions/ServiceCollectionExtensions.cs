using Inventory.Application.Abstractions;
using Inventory.Application.Categories.Commands;
using Inventory.Application.Categories.Commands.Handlers;
using Inventory.Application.Categories.Models;
using Inventory.Application.Categories.Queries;
using Inventory.Application.Categories.Queries.Handlers;
using Inventory.Application.InventoryMovements.Commands;
using Inventory.Application.InventoryMovements.Commands.Handlers;
using Inventory.Application.InventoryMovements.Models;
using Inventory.Application.Products.Commands;
using Inventory.Application.Products.Commands.Handlers;
using Inventory.Application.Products.Models;
using Inventory.Application.Products.Queries;
using Inventory.Application.Products.Queries.Handlers;

namespace Inventory.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationHandlers(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateProductCommand, int>, CreateProductCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateProductCommand, bool>, UpdateProductCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteProductCommand, bool>, DeleteProductCommandHandler>();
        services.AddScoped<IQueryHandler<GetProductQuery, ProductResult>, GetProductQueryHandler>();
        services.AddScoped<IQueryHandler<ListProductsQuery, IReadOnlyList<ProductResult>>, ListProductsQueryHandler>();
        services.AddScoped<ICommandHandler<CreateCategoryCommand, int>, CreateCategoryCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateCategoryCommand, bool>, UpdateCategoryCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteCategoryCommand, bool>, DeleteCategoryCommandHandler>();
        services.AddScoped<IQueryHandler<GetCategoryQuery, CategoryResult>, GetCategoryQueryHandler>();
        services.AddScoped<IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResult>>, ListCategoriesQueryHandler>();
        services.AddScoped<ICommandHandler<RegisterMovementCommand, MovementResult>, RegisterMovementCommandHandler>();

        return services;
    }
}
