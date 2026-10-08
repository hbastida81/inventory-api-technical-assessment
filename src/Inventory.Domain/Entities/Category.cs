namespace Inventory.Domain.Entities;

public class Category
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Category(string name)
    {
        ValidateName(name);
        Name = name;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    private Category() { Name = string.Empty; }

    public void Update(string name)
    {
        ValidateName(name);
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("Name must not exceed 200 characters.", nameof(name));
    }
}
