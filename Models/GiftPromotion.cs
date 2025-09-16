using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class GiftPromotion
{
    public Guid Id { get; set; }

    [Required, StringLength(100)] public string GiftName { get; set; }

    public string Description { get; set; }

    public Guid RequiredProductId { get; set; }
    public Product? RequiredProduct { get; set; }

    public Guid GiftProductId { get; set; }
    public Product? GiftProduct { get; set; }

    [Range(1, 100)] public int QuantityRequired { get; set; } = 1;

    [Range(1, 100)] public int QuantityGift { get; set; } = 1;

    [DataType(DataType.Date)] public DateTime StartDate { get; set; }

    [DataType(DataType.Date)] public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;
}