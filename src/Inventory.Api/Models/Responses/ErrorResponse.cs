namespace Inventory.Api.Models.Responses;

public class ErrorResponse
{
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Errors { get; set; }
}
