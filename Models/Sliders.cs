using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyWebApp.Extensions.Validation;

namespace MyWebApp.Models;

public class Sliders
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;
    
    public int? Status { get; set; } = 1;

    public string? Image { get; set; }

    [NotMapped]
    [FileExtension(ErrorMessage = "Chỉ nhận tệp ảnh có đuôi jpg, png, jpeg")]
    public IFormFile? ImageFile { get; set; }

    [NotMapped] public string? ImageUrl { get; set; }
}