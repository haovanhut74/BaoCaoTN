using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using MyWebApp.Extensions;
using MyWebApp.Extensions.Validation;

namespace MyWebApp.Models;

public class Product
{
    [Key] public Guid Id { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Mô tả sản phẩm không được để trống")]
    public string Description { get; set; }


    public string? Slug { get; set; }

    [Required(ErrorMessage = "Giá sản phẩm không được để trống")]
    [Range(1, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn hoặc bằng 1")]
    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; } // Giá sau giảm (nếu có)
    public int? DiscountPercent { get; set; }

    [Required(ErrorMessage = "Số lượng sản phẩm không được để trống")]
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng sản phẩm phải lớn hơn hoặc bằng 0")]
    public int Quantity { get; set; }

    public int Sold { get; set; } = 0;

    [Required(ErrorMessage = "Vui lòng chọn danh mục sản phẩm")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu sản phẩm")]
    public Guid BrandId { get; set; }

    public string? MainImage { get; set; }
    public List<ProductImage> Images { get; set; } = []; // nhiều ảnh

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public List<Comment> Comments { get; set; } = new();
    public List<ProductSpecification> Specifications { get; set; } = [];

    [NotMapped]
    [FileExtension(ErrorMessage = "Chỉ nhận tệp ảnh có đuôi jpg, png, jpeg")]
    public IFormFile? ImageFile { get; set; }

    [NotMapped] public string? ImageUrl { get; set; }
    
}