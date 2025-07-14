using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class Product
{
    [Key] public Guid Id { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    public string Name { get; set; } // Tên sản phẩm

    [Required(ErrorMessage = "Mô tả sản phẩm không được để trống")]
    public string Description { get; set; } // Mô tả sản phẩm

    public string Slug { get; set; } // Đường dẫn sản phẩm

    [Required(ErrorMessage = "Giá sản phẩm không được để trống"),
     Range(1, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn hoặc bằng 1")]
    public decimal Price { get; set; } // Giá sản phẩm

    [Required(ErrorMessage = "Số lượng sản phẩm không được để trống"),
     Range(0, int.MaxValue, ErrorMessage = "Số lượng sản phẩm phải lớn hơn hoặc bằng 0")]
    public int Quantity { get; set; } // Số lượng sản phẩm

    [Required(ErrorMessage = "Hình ảnh sản phẩm không được để trống")]
    public string Image { get; set; } // Hình ảnh sản phẩm

    public DateTime CreatedAt { get; set; } // Ngày tạo sản phẩm

    public Guid CategoryId { get; set; } // ID danh mục sản phẩm
    public Guid BrandId { get; set; } // ID thương hiệu sản phẩm
    public Category Category { get; set; } // Danh mục sản phẩm
    public Brand Brand { get; set; } // Thương hiệu sản phẩm
}