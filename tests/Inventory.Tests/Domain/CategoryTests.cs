using Inventory.Domain.Entities;

namespace Inventory.Tests.Domain;

public class CategoryTests
{
    [Fact]
    public void Constructor_ValidName_CreatesCategory()
    {
        var category = new Category("Electronics");
        Assert.Equal("Electronics", category.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Category(name!));
    }

    [Fact]
    public void Constructor_NameExceeds200Characters_ThrowsArgumentException()
    {
        var longName = new string('A', 201);
        Assert.Throws<ArgumentException>(() => new Category(longName));
    }

    [Fact]
    public void Update_ValidName_UpdatesName()
    {
        var category = new Category("Old Name");
        category.Update("New Name");
        Assert.Equal("New Name", category.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_InvalidName_ThrowsArgumentException(string? name)
    {
        var category = new Category("Valid");
        Assert.Throws<ArgumentException>(() => category.Update(name!));
    }

    [Fact]
    public void Update_NameExceeds200Characters_ThrowsArgumentException()
    {
        var category = new Category("Valid");
        var longName = new string('B', 201);
        Assert.Throws<ArgumentException>(() => category.Update(longName));
    }
}
