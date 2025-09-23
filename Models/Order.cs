using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class Order
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; }
    public string UserName { get; set; }
    public DateTime OrderDate { get; set; }
    public int Status { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public string Address { get; set; }
    public List<OrderDetail> OrderDetails { get; set; } = new();

    [Required]
    [Display(Name = "Địa chỉ chi tiết")]
    public string FullAddress { get; set; } = string.Empty;

    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; }

    [Required]
    [Phone]
    [StringLength(11, MinimumLength = 10)]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    // Nếu bạn muốn lưu raw response JSON của GHN
    public string? GhnResponse { get; set; }
    public string? GHNOrderCode { get; set; } // mã đơn GHN (VD: L3EVY8)
}