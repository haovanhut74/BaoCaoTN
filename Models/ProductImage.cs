using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class ProductImage
{
    [Key] public Guid Id { get; set; }
    [Required] public Guid ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    public Product? Product { get; set; }
}