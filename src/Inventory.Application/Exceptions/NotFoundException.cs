namespace Inventory.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} with identifier '{id}' was not found.") { }
}
