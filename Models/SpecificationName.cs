using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class SpecificationName
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    // Liên kết ngược (tùy chọn)
    public List<ProductSpecification> ProductSpecifications { get; set; } = new();
}