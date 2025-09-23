using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class Shiping
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "Thành phố")]
    public string City { get; set; }

    [Required]
    [Display(Name = "Quận/Huyện")]
    public string District { get; set; }

    [Required]
    [Range(0, 1000000)]
    [Display(Name = "Phí vận chuyển")]
    public decimal Price { get; set; }
}