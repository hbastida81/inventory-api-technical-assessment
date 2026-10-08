using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Infrastructure.Persistence;

namespace Inventory.Tests.Api;

public class ApiValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiValidationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=fake;Database=fake;",
                    ["Authentication:Authority"] = "http://localhost:8080/realms/inventory",
                    ["Authentication:Audience"] = "inventory-api",
                    ["Authentication:RequireHttpsMetadata"] = "false"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<InventoryDbContext>(options => options.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task CreateProduct_InvalidRequest_ReturnsConsistentErrorContract()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new { Name = "", CategoryId = 1, Price = -5.0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Equal("One or more validation errors occurred.", root.GetProperty("message").GetString());
        Assert.True(root.TryGetProperty("errors", out var errors));
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.False(root.TryGetProperty("type", out _));
        Assert.False(root.TryGetProperty("title", out _));
        Assert.False(root.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task CreateCategory_EmptyName_ReturnsConsistentErrorContract()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new { Name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.True(root.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Name", out _));
    }

    [Fact]
    public async Task CreateMovement_InvalidQuantity_ReturnsConsistentErrorContract()
    {
        var response = await _client.PostAsJsonAsync("/api/products/1/movements", new { Type = "Entry", Quantity = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.True(root.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task SwaggerDocument_ContainsDocumentedResponses()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");
        var createProduct = paths.GetProperty("/api/Products").GetProperty("post").GetProperty("responses");
        Assert.True(createProduct.TryGetProperty("201", out _));
        Assert.True(createProduct.TryGetProperty("400", out _));
        Assert.True(createProduct.TryGetProperty("401", out _));
        var deleteProduct = paths.GetProperty("/api/Products/{id}").GetProperty("delete").GetProperty("responses");
        Assert.True(deleteProduct.TryGetProperty("204", out _));
        Assert.True(deleteProduct.TryGetProperty("409", out _));
        var movement = paths.GetProperty("/api/products/{productId}/movements").GetProperty("post").GetProperty("responses");
        Assert.True(movement.TryGetProperty("201", out _));
        Assert.True(movement.TryGetProperty("409", out _));
    }
}
