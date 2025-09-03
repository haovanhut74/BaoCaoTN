using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class ProductSpecification
{
    [Key] public Guid Id { get; set; }

    [Required] public Guid ProductId { get; set; }
    public Product Product { get; set; }

    // Dùng SpecNameId nếu chọn từ danh sách
    public Guid? SpecNameId { get; set; }
    public SpecificationName? SpecName { get; set; }

    // Key tự do nếu muốn thêm mới
    public string? Key { get; set; }

    [Required] public string Value { get; set; }

    // Thời gian bảo hành (tùy chọn)
    public int? WarrantyMonths { get; set; }
}