using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class Brand
{
    [Key] public Guid Id { get; set; }

    [Required(ErrorMessage = "Tên thương hiệu không được để trống")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Mô tả không được để trống")]
    public string Description { get; set; }

    public string Slug { get; set; }

    public int Status { get; set; } // 1: Hiện, 0: Ẩn
}