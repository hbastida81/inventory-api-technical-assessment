using System.ComponentModel.DataAnnotations;

namespace Inventory.Api.Models.Requests;

public class CreateCategoryRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
