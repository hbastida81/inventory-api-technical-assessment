using System.ComponentModel.DataAnnotations;

namespace Inventory.Api.Models.Requests;

public class CreateMovementRequest
{
    [Required]
    public string Type { get; set; } = string.Empty;
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive integer.")]
    public int Quantity { get; set; }
}
