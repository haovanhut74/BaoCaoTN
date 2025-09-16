using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class News
{
    public Guid Id { get; set; }

    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;

    [Required] public string Content { get; set; } = string.Empty;

    [StringLength(500)] public string? ThumbnailUrl { get; set; } // ảnh đại diện

    [StringLength(200)] public string? Author { get; set; } = "Admin";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}