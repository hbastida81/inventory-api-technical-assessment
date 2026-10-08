using System.ComponentModel.DataAnnotations;

namespace Inventory.Api.Models.Requests;

public class UpdateProductRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required]
    public int CategoryId { get; set; }
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to zero.")]
    public decimal Price { get; set; }
}
