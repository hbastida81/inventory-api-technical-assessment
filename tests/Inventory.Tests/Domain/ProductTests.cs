using Inventory.Domain.Entities;

namespace Inventory.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesProduct()
    {
        var product = new Product("Laptop", 1, 999.99m);
        Assert.Equal("Laptop", product.Name);
        Assert.Equal(1, product.CategoryId);
        Assert.Equal(999.99m, product.Price);
        Assert.Equal(0, product.Stock);
    }

    [Fact]
    public void Constructor_ZeroPrice_CreatesProduct()
    {
        var product = new Product("Free Item", 1, 0m);
        Assert.Equal(0m, product.Price);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Product(name!, 1, 10m));
    }

    [Fact]
    public void Constructor_NameExceeds200Characters_ThrowsArgumentException()
    {
        var longName = new string('A', 201);
        Assert.Throws<ArgumentException>(() => new Product(longName, 1, 10m));
    }

    [Fact]
    public void Constructor_NegativePrice_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Product("Item", 1, -1m));
    }

    [Fact]
    public void AddStock_PositiveQuantity_IncreasesStock()
    {
        var product = new Product("Item", 1, 10m);
        product.AddStock(5);
        Assert.Equal(5, product.Stock);
    }

    [Fact]
    public void AddStock_MultipleCalls_AccumulatesStock()
    {
        var product = new Product("Item", 1, 10m);
        product.AddStock(3);
        product.AddStock(7);
        Assert.Equal(10, product.Stock);
    }

    [Fact]
    public void RemoveStock_SufficientStock_DecreasesStock()
    {
        var product = new Product("Item", 1, 10m);
        product.AddStock(10);
        product.RemoveStock(4);
        Assert.Equal(6, product.Stock);
    }

    [Fact]
    public void RemoveStock_ExactStock_SetsStockToZero()
    {
        var product = new Product("Item", 1, 10m);
        product.AddStock(5);
        product.RemoveStock(5);
        Assert.Equal(0, product.Stock);
    }

    [Fact]
    public void RemoveStock_ExceedsStock_ThrowsInvalidOperationException()
    {
        var product = new Product("Item", 1, 10m);
        product.AddStock(3);
        Assert.Throws<InvalidOperationException>(() => product.RemoveStock(5));
    }

    [Fact]
    public void RemoveStock_ZeroStock_ThrowsInvalidOperationException()
    {
        var product = new Product("Item", 1, 10m);
        Assert.Throws<InvalidOperationException>(() => product.RemoveStock(1));
    }

    [Fact]
    public void Update_ValidParameters_UpdatesProperties()
    {
        var product = new Product("Old Name", 1, 10m);
        product.Update("New Name", 2, 20m);
        Assert.Equal("New Name", product.Name);
        Assert.Equal(2, product.CategoryId);
        Assert.Equal(20m, product.Price);
    }

    [Fact]
    public void Update_DoesNotAffectStock()
    {
        var product = new Product("Item", 1, 10m);
        product.AddStock(5);
        product.Update("Updated", 2, 20m);
        Assert.Equal(5, product.Stock);
    }
}
