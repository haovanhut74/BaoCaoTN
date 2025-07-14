using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class Category
{
    [Key] public Guid Id { get; set; }

    [Required(ErrorMessage = "Tên danh mục không được để trống")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Mô tả không được để trống")]
    public string Description { get; set; }

    public string Slug { get; set; }
    public int status { get; set; } // 1: Hiện, 0: Ẩn
}