using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class DiscountCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(50)]
    [Display(Name = "Mã giảm giá")]
    public string Code { get; set; }

    [Display(Name = "Giảm theo số tiền")]
    [Range(0, 1000000)]
    public decimal DiscountAmount { get; set; }

    [Display(Name = "Giảm theo phần trăm")]
    [Range(0, 100)]
    public double? DiscountPercent { get; set; }

    [Display(Name = "Ngày bắt đầu")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Display(Name = "Ngày kết thúc")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Display(Name = "Kích hoạt")] public bool IsActive { get; set; } = true;

    [Display(Name = "Số lượt sử dụng tối đa")]
    public int UsageLimit { get; set; } = 0; // 0 = không giới hạn

    [Display(Name = "Số lượt đã sử dụng")] public int UsedCount { get; set; } = 0;
}