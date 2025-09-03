using Microsoft.AspNetCore.Mvc.Rendering;
using MyWebApp.Models;

namespace MyWebApp.ViewModels;

public class ProductSpecificationEditViewModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public SpecificationName? Name { get; set; } // Tên thông số
    public string Value { get; set; } = string.Empty; // Giá trị thông số
    public int? WarrantyMonths { get; set; }

    // Danh sách SpecificationNames để dropdown
    public List<SelectListItem> SpecNames { get; set; } = new();
    public Guid? SpecNameId { get; set; }
}