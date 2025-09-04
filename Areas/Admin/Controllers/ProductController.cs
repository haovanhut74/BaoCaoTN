using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Areas.Permission;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;
using OfficeOpenXml; // NuGet: EPPlus
using iTextSharp.text; // NuGet: iTextSharp.LGPLv2.Core
using iTextSharp.text.pdf;

namespace MyWebApp.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission("ManageProducts")]
public class ProductController : BaseController
{
    private readonly IWebHostEnvironment _env;

    public ProductController(DataContext context, IWebHostEnvironment env) : base(context)
    {
        _env = env;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        int pageSize = 10; // Số sản phẩm hiển thị trên mỗi trang

        var totalItems = await _context.Products.CountAsync();

        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new ListViewModel
        {
            Products = products,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
        };

        return View(viewModel);
    }


    [HttpGet]
    [HasPermission("CreateProduct")]
    public IActionResult Create()
    {
        ViewBag.Categories = new SelectList(_context.Categories.ToList(), "Id", "Name");
        ViewBag.Brands = new SelectList(_context.Brands.ToList(), "Id", "Name");
        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("CreateProduct")]
    public async Task<IActionResult> Create(Product product)
    {
        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);

        if (product.ImageFile != null)
        {
            // Upload ảnh từ file như cũ
            string uploadDir = Path.Combine(_env.WebRootPath, "img/product");
            string imgName = Guid.NewGuid().ToString() + "_" + product.ImageFile.FileName;
            string filePath = Path.Combine(uploadDir, imgName);

            await using FileStream fs = new(filePath, FileMode.Create);
            await product.ImageFile.CopyToAsync(fs);
            product.MainImage = imgName;
        }
        else if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            // Tải ảnh từ URL
            try
            {
                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(product.ImageUrl);

                string ext = Path.GetExtension(product.ImageUrl).Split('?')[0]; // tránh query string
                string imgName = Guid.NewGuid().ToString() + ext;
                string filePath = Path.Combine(_env.WebRootPath, "img/product", imgName);

                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                product.MainImage = imgName;
            }
            catch (Exception)
            {
                ModelState.AddModelError("ImageUrl", "Không thể tải ảnh từ liên kết.");
            }
        }
        else
        {
            ModelState.AddModelError("ImageFile", "Vui lòng tải ảnh hoặc nhập link ảnh.");
        }

        product.Slug = product.Name.Replace(" ", "-").ToLower();
        var slug = await _context.Products.Where(p => p.Slug == product.Slug).FirstOrDefaultAsync();
        if (slug != null)
        {
            ModelState.AddModelError("", "Sản phẩm đã có trong Data");
            return View(product);
        }
        if (ModelState.IsValid)
        {
            // Tính giá sau giảm
            if (product.DiscountPercent.HasValue)
                product.DiscountPrice = Math.Round(product.Price * (100 - product.DiscountPercent.Value) / 100);
            else
                product.DiscountPrice = product.Price;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            TempData["message"] = "Thêm mới thành công";
            return RedirectToAction("Index");
        }

        TempData["error"] = "Thêm mới thất bại";
        List<string> errors = [];
        foreach (var item in ModelState.Values)
        {
            foreach (var error in item.Errors)
            {
                errors.Add(error.ErrorMessage);
            }
        }

        string errorMessage = string.Join("\n", errors);
        return BadRequest(errorMessage);
    }

    [HttpGet]
    [HasPermission("EditProduct")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            return View(product);
        }

        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("EditProduct")]
    public async Task<IActionResult> Edit(Guid id, Product product)
    {
        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);

        var oldProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (oldProduct == null)
        {
            return NotFound();
        }

        string uploadDir = Path.Combine(_env.WebRootPath, "img/product");

        // Trường hợp có file upload
        if (product.ImageFile is { Length: > 0 })
        {
            // Xóa ảnh cũ nếu có
            if (!string.IsNullOrEmpty(oldProduct.MainImage))
            {
                string oldPath = Path.Combine(uploadDir, oldProduct.MainImage);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            // Lưu ảnh mới
            string newImgName = Guid.NewGuid() + Path.GetExtension(product.ImageFile.FileName);
            string newFilePath = Path.Combine(uploadDir, newImgName);
            await using var fs = new FileStream(newFilePath, FileMode.Create);
            await product.ImageFile.CopyToAsync(fs);
            product.MainImage = newImgName;
        }
        // Trường hợp có link ảnh và không upload file
        else if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            try
            {
                using var httpClient = new HttpClient();
                var imgBytes = await httpClient.GetByteArrayAsync(product.ImageUrl);

                string ext = Path.GetExtension(product.ImageUrl).ToLowerInvariant();
                if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(ext))
                {
                    ModelState.AddModelError("ImageUrl", "Định dạng ảnh không hợp lệ.");
                    return View(product);
                }

                // Xóa ảnh cũ
                if (!string.IsNullOrEmpty(oldProduct.MainImage))
                {
                    string oldPath = Path.Combine(uploadDir, oldProduct.MainImage);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                string newImgName = Guid.NewGuid() + ext;
                string savePath = Path.Combine(uploadDir, newImgName);
                await System.IO.File.WriteAllBytesAsync(savePath, imgBytes);

                product.MainImage = newImgName;
            }
            catch
            {
                ModelState.AddModelError("ImageUrl", "Không thể tải ảnh từ đường dẫn.");
                return View(product);
            }
        }
        else
        {
            // Không có file, không có link -> giữ nguyên ảnh cũ
            product.MainImage = oldProduct.MainImage;
        }

        // Slug kiểm tra trùng
        product.Slug = product.Name.Replace(" ", "-").ToLower();
        var slugExists = await _context.Products
            .AnyAsync(p => p.Slug == product.Slug && p.Id != id);
        if (slugExists)
        {
            ModelState.AddModelError("", "Sản phẩm đã tồn tại.");
            return View(product);
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (product.DiscountPercent.HasValue)
                    product.DiscountPrice = Math.Round(product.Price * (100 - product.DiscountPercent.Value) / 100);
                else
                    product.DiscountPrice = product.Price;

                _context.Products.Update(product); 
                await _context.SaveChangesAsync();
                TempData["message"] = "Thêm mới thành công";
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(p => p.Id == id))
                    return NotFound();
                throw;
            }
        }

        TempData["error"] = "Cập nhật thất bại";
        return View(product);
    }

    [HttpGet]
    [HasPermission("DeleteProduct")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            // Xoá file ảnh (nếu có)
            if (!string.IsNullOrEmpty(product.MainImage))
            {
                string uploadDir = Path.Combine(_env.WebRootPath, "img/product");
                string imgName = product.MainImage.Replace("img/product/", "").Replace("img/product", "");
                string filePath = Path.Combine(uploadDir, imgName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        return NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> AddImage(Guid productId, IFormFile? imageFile, string? imageUrl)
    {
        if ((imageFile == null || imageFile.Length == 0) && string.IsNullOrEmpty(imageUrl))
        {
            ModelState.AddModelError("", "Vui lòng chọn file hoặc nhập link ảnh.");
            return RedirectToAction(nameof(Index), new { id = productId });
        }

        string uploadDir = Path.Combine(_env.WebRootPath, "img/product");
        string fileName = "";

        if (imageFile != null && imageFile.Length > 0)
        {
            fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(uploadDir, fileName);

            await using var fs = new FileStream(filePath, FileMode.Create);
            await imageFile.CopyToAsync(fs);
        }
        else if (!string.IsNullOrEmpty(imageUrl))
        {
            try
            {
                using var httpClient = new HttpClient();
                var imgBytes = await httpClient.GetByteArrayAsync(imageUrl);

                string ext = Path.GetExtension(imageUrl).Split('?')[0].ToLower();
                if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(ext))
                {
                    ModelState.AddModelError("", "Định dạng ảnh không hợp lệ.");
                    return RedirectToAction(nameof(Index), new { id = productId });
                }

                fileName = Guid.NewGuid() + ext;
                string savePath = Path.Combine(uploadDir, fileName);
                await System.IO.File.WriteAllBytesAsync(savePath, imgBytes);
            }
            catch
            {
                ModelState.AddModelError("", "Không thể tải ảnh từ link.");
                return RedirectToAction(nameof(Index), new { id = productId });
            }
        }

        _context.ProductImages.Add(new ProductImage
        {
            ProductId = productId,
            ImageUrl = "/img/product/" + fileName // ✅ Lưu luôn path
        });

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { id = productId });
    }

    [HttpPost]
    [HasPermission("ManageProducts")]
    public async Task<IActionResult> DeleteImage(Guid imageId)
    {
        var img = await _context.ProductImages.FindAsync(imageId);
        if (img != null)
        {
            string path = Path.Combine(_env.WebRootPath, "img/product", img.ImageUrl);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { id = img.ProductId });
        }

        return NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv()
    {
        var products = await _context.Products.Include(p => p.Category).Include(p => p.Brand).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Tên sản phẩm,Giá,Số lượng,Danh mục,Thương hiệu");

        foreach (var p in products)
        {
            sb.AppendLine($"{p.Name},{p.Price},{p.Quantity},{p.Category?.Name},{p.Brand?.Name}");
        }

        var csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(csvBytes, "text/csv", "Products.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var products = await _context.Products.Include(p => p.Category).Include(p => p.Brand).ToListAsync();

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Products");

        // Header
        worksheet.Cells[1, 1].Value = "Tên sản phẩm";
        worksheet.Cells[1, 2].Value = "Giá";
        worksheet.Cells[1, 3].Value = "Số lượng";
        worksheet.Cells[1, 4].Value = "Danh mục";
        worksheet.Cells[1, 5].Value = "Thương hiệu";

        int row = 2;
        foreach (var p in products)
        {
            worksheet.Cells[row, 1].Value = p.Name;
            worksheet.Cells[row, 2].Value = p.Price;
            worksheet.Cells[row, 3].Value = p.Quantity;
            worksheet.Cells[row, 4].Value = p.Category?.Name;
            worksheet.Cells[row, 5].Value = p.Brand?.Name;
            row++;
        }

        var stream = new MemoryStream(await package.GetAsByteArrayAsync());
        stream.Position = 0; // reset vị trí stream trước khi trả về
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Products.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var products = await _context.Products.Include(p => p.Category).Include(p => p.Brand).ToListAsync();

        using var stream = new MemoryStream();
        var document = new Document(PageSize.A4.Rotate());
        var writer = PdfWriter.GetInstance(document, stream);
        document.Open();

        // Font path
        BaseFont baseFont;
        string fontPath = Path.Combine(_env.WebRootPath, "fonts", "tahoma.ttf");

        if (System.IO.File.Exists(fontPath))
        {
            baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
        }
        else
        {
            baseFont = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
        }

        var font = new Font(baseFont, 12);

        var table = new PdfPTable(6) { WidthPercentage = 100 };
        table.SetWidths(new float[] { 1f, 3f, 2f, 1.5f, 2f, 2f });

        table.AddCell(new PdfPCell(new Phrase("Image", font)) { HorizontalAlignment = Element.ALIGN_CENTER });
        table.AddCell(new PdfPCell(new Phrase("Product Name", font)) { HorizontalAlignment = Element.ALIGN_CENTER });
        table.AddCell(new PdfPCell(new Phrase("Price", font)) { HorizontalAlignment = Element.ALIGN_CENTER });
        table.AddCell(new PdfPCell(new Phrase("Quantity", font)) { HorizontalAlignment = Element.ALIGN_CENTER });
        table.AddCell(new PdfPCell(new Phrase("Category", font)) { HorizontalAlignment = Element.ALIGN_CENTER });
        table.AddCell(new PdfPCell(new Phrase("Brand", font)) { HorizontalAlignment = Element.ALIGN_CENTER });

        foreach (var p in products)
        {
            if (!string.IsNullOrEmpty(p.MainImage))
            {
                string imgPath = Path.Combine(_env.WebRootPath, "img/product", p.MainImage);
                if (System.IO.File.Exists(imgPath))
                {
                    try
                    {
                        byte[] imgBytes = await System.IO.File.ReadAllBytesAsync(imgPath);
                        var img = Image.GetInstance(imgBytes);
                        img.ScaleAbsolute(50f, 50f);
                        var cell = new PdfPCell(img)
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            Padding = 5
                        };
                        table.AddCell(cell);
                    }
                    catch
                    {
                        table.AddCell(new PdfPCell(new Phrase("Image Error", font))
                            { HorizontalAlignment = Element.ALIGN_CENTER });
                    }
                }
                else
                {
                    table.AddCell(new PdfPCell(new Phrase("No Image", font))
                        { HorizontalAlignment = Element.ALIGN_CENTER });
                }
            }
            else
            {
                table.AddCell(new PdfPCell(new Phrase("No Image", font))
                    { HorizontalAlignment = Element.ALIGN_CENTER });
            }

            table.AddCell(new PdfPCell(new Phrase(p.Name, font)));
            table.AddCell(new PdfPCell(new Phrase(p.Price.ToString("N0"), font))
                { HorizontalAlignment = Element.ALIGN_RIGHT });
            table.AddCell(new PdfPCell(new Phrase(p.Quantity.ToString(), font))
                { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(p.Category?.Name ?? "", font)));
            table.AddCell(new PdfPCell(new Phrase(p.Brand?.Name ?? "", font)));
        }

        document.Add(table);
        document.Close();

        stream.Position = 0;
        return File(stream.ToArray(), "application/pdf", "Products.pdf");
    }
}