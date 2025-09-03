// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using MyWebApp.Data;
// using MyWebApp.Models;
//
// namespace MyWebApp.Areas.Admin.Controllers;
//
// public class ProductImageController : BaseController
// {
//     private readonly IWebHostEnvironment _env;
//
//     public ProductImageController(DataContext context, IWebHostEnvironment env) : base(context)
//     {
//         _env = env;
//     }
//
//     [HttpGet]
//     public async Task<IActionResult> Index(Guid productId)
//     {
//         var product = await _context.Products.Include(p => p.Images)
//             .FirstOrDefaultAsync(p => p.Id == productId);
//         if (product == null) return NotFound();
//         ViewBag.Product = product;
//         return View(product.Images);
//     }
//
//     [HttpPost]
//     public async Task<IActionResult> Upload(Guid productId, IFormFile file)
//     {
//         if (file != null && file.Length > 0)
//         {
//             var folder = Path.Combine(_env.WebRootPath, "img/product");
//             var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
//             var path = Path.Combine(folder, fileName);
//
//             using (var stream = new FileStream(path, FileMode.Create))
//             {
//                 await file.CopyToAsync(stream);
//             }
//
//             var img = new ProductImage
//             {
//                 ProductId = productId,
//                 ImageUrl = fileName
//             };
//             _context.ProductImages.Add(img);
//             await _context.SaveChangesAsync();
//         }
//
//         return RedirectToAction("Index", new { productId });
//     }
//
//     [HttpPost]
//     public async Task<IActionResult> Delete(Guid id)
//     {
//         var img = await _context.ProductImages.FindAsync(id);
//         if (img != null)
//         {
//             var path = Path.Combine(_env.WebRootPath, "img/product", img.ImageUrl);
//             if (System.IO.File.Exists(path))
//                 System.IO.File.Delete(path);
//
//             _context.ProductImages.Remove(img);
//             await _context.SaveChangesAsync();
//         }
//
//         return RedirectToAction("Index", new { productId = img?.ProductId });
//     }
// }