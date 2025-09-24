using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyWebApp.Models;

public class CartItem
{
    [Key] public Guid Id { get; set; }

    [Required] public Guid CartId { get; set; }

    [ForeignKey("CartId")] public Cart Cart { get; set; }

    [Required] public Guid ProductId { get; set; }

    [ForeignKey("ProductId")] public Product Product { get; set; }

    [Range(1, int.MaxValue)] public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; }

    [NotMapped] public decimal TotalPrice => Price * Quantity;
    
    public bool IsGift { get; set; } = false;
    
    // Nếu muốn trace promotion
    public Guid? GiftPromotionId { get; set; }
}