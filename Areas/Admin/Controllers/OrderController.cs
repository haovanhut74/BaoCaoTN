using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Areas.Permission;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;
using OfficeOpenXml;

namespace MyWebApp.Areas.Admin.Controllers;

[HasPermission("ManageOrders")]
public class OrderController : BaseController
{
    private readonly IWebHostEnvironment _env;

    public OrderController(DataContext context, IWebHostEnvironment env) : base(context)
    {
        _env = env;
    }

    // Hiển thị danh sách đơn hàng
    public async Task<IActionResult> Index(string? orderCode, string? userName, string? status, string? dateFrom,
        string? dateTo, int page = 1)
    {
        var query = _context.Orders.Include(o => o.OrderDetails).AsQueryable();

        if (!string.IsNullOrWhiteSpace(orderCode))
            query = query.Where(o => o.OrderCode.Contains(orderCode));

        if (!string.IsNullOrWhiteSpace(userName))
            query = query.Where(o => o.UserName.Contains(userName));

        if (!string.IsNullOrWhiteSpace(status) && int.TryParse(status, out int s))
            query = query.Where(o => o.Status == s);

        if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
            query = query.Where(o => o.OrderDate.Date >= fromDate.Date);

        if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out var toDate))
        {
            toDate = toDate.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.OrderDate <= toDate);
        }

        int pageSize = 10;
        int totalItems = await query.CountAsync();
        var orders = await query.OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new ListViewModel
        {
            Orders = orders,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
            OrderCode = orderCode,
            UserName = userName,
            Status = status,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        return View(viewModel);
    }


    // Chi tiết đơn hàng
    [HasPermission("ViewOrder")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product) //❗ cần include Product
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null) return NotFound();

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, int newStatus)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = newStatus;
        await _context.SaveChangesAsync();

        return RedirectToAction("Detail", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("ManageOrders")] // hoặc quyền phù hợp
    public async Task<IActionResult> Delete(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
            return NotFound();

        // Xóa chi tiết đơn trước (nếu cascade chưa bật trong DB)
        if (order.OrderDetails != null && order.OrderDetails.Any())
        {
            _context.OrderDetails.RemoveRange(order.OrderDetails);
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã xóa đơn hàng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? orderCode, string? userName, string? status, string? dateFrom,
        string? dateTo)
    {
        var query = FilterOrders(orderCode, userName, status, dateFrom, dateTo);
        var orders = await query.Include(o => o.OrderDetails).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Mã đơn hàng,Người đặt,Ngày đặt,Trạng thái,Số sản phẩm,Tổng tiền");

        foreach (var order in orders)
        {
            string statusLabel = GetStatusLabel(order.Status);
            int productCount = order.OrderDetails?.Count ?? 0;
            decimal totalPrice = order.OrderDetails?.Sum(od => od.Quantity * od.Price) ?? 0;

            sb.AppendLine(
                $"{order.OrderCode},{order.UserName},{order.OrderDate:dd/MM/yyyy HH:mm},{statusLabel},{productCount},{totalPrice:N0}");
        }

        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "Orders.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string? orderCode, string? userName, string? status, string? dateFrom,
        string? dateTo)
    {
        var query = FilterOrders(orderCode, userName, status, dateFrom, dateTo);
        var orders = await query.Include(o => o.OrderDetails).ToListAsync();

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Orders");

        // Header
        string[] headers = { "Mã đơn hàng", "Người đặt", "Ngày đặt", "Trạng thái", "Số sản phẩm", "Tổng tiền" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Column(i + 1).AutoFit();
        }

        int row = 2;
        foreach (var order in orders)
        {
            string statusLabel = GetStatusLabel(order.Status);
            int productCount = order.OrderDetails?.Count ?? 0;
            decimal totalPrice = order.OrderDetails?.Sum(od => od.Quantity * od.Price) ?? 0;

            worksheet.Cells[row, 1].Value = order.OrderCode;
            worksheet.Cells[row, 2].Value = order.UserName;
            worksheet.Cells[row, 3].Value = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
            worksheet.Cells[row, 4].Value = statusLabel;
            worksheet.Cells[row, 5].Value = productCount;
            worksheet.Cells[row, 6].Value = totalPrice;
            row++;
        }

        var stream = new MemoryStream(await package.GetAsByteArrayAsync());
        stream.Position = 0;
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Orders.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? orderCode, string? userName, string? status, string? dateFrom,
        string? dateTo)
    {
        var query = FilterOrders(orderCode, userName, status, dateFrom, dateTo);
        var orders = await query.Include(o => o.OrderDetails).ToListAsync();

        using var stream = new MemoryStream();
        var document = new Document(PageSize.A4.Rotate(), 10f, 10f, 10f, 10f);
        var writer = PdfWriter.GetInstance(document, stream);
        document.Open();

        BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        var font = new Font(baseFont, 10);

        var table = new PdfPTable(6) { WidthPercentage = 100 };
        table.SetWidths(new float[] { 2f, 3f, 3f, 2f, 2f, 2f });

        // Header
        BaseColor lightGray = new BaseColor(211, 211, 211);
        string[] headers = { "Mã đơn hàng", "Người đặt", "Ngày đặt", "Trạng thái", "Số sản phẩm", "Tổng tiền" };
        foreach (var h in headers)
        {
            table.AddCell(new PdfPCell(new Phrase(h, font))
                { BackgroundColor = lightGray, HorizontalAlignment = Element.ALIGN_CENTER });
        }

        // Rows
        foreach (var order in orders)
        {
            string statusLabel = GetStatusLabel(order.Status);
            int productCount = order.OrderDetails?.Count ?? 0;
            decimal totalPrice = order.OrderDetails?.Sum(od => od.Quantity * od.Price) ?? 0;

            table.AddCell(new PdfPCell(new Phrase(order.OrderCode, font)));
            table.AddCell(new PdfPCell(new Phrase(order.UserName, font)));
            table.AddCell(new PdfPCell(new Phrase(order.OrderDate.ToString("dd/MM/yyyy HH:mm"), font)));
            table.AddCell(new PdfPCell(new Phrase(statusLabel, font)));
            table.AddCell(new PdfPCell(new Phrase(productCount.ToString(), font))
                { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(totalPrice.ToString("N0"), font))
                { HorizontalAlignment = Element.ALIGN_RIGHT });
        }

        document.Add(table);
        document.Close();

        return File(stream.ToArray(), "application/pdf", "Orders.pdf");
    }


    // Hàm helper lọc đơn hàng theo tham số filter giống trong Index
    private IQueryable<Order> FilterOrders(string? orderCode, string? userName, string? status, string? dateFrom,
        string? dateTo)
    {
        var query = _context.Orders.Include(o => o.OrderDetails).AsQueryable();

        if (!string.IsNullOrWhiteSpace(orderCode))
            query = query.Where(o => o.OrderCode.Contains(orderCode));

        if (!string.IsNullOrWhiteSpace(userName))
            query = query.Where(o => o.UserName.Contains(userName));

        if (!string.IsNullOrWhiteSpace(status) && int.TryParse(status, out int s))
            query = query.Where(o => o.Status == s);

        if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
            query = query.Where(o => o.OrderDate.Date >= fromDate.Date);

        if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out var toDate))
        {
            toDate = toDate.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.OrderDate <= toDate);
        }

        return query;
    }

    [HttpGet]
    [HasPermission("ViewOrder")] // hoặc permission phù hợp
    public async Task<IActionResult> PrintOrder(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null) return NotFound();

        using var stream = new MemoryStream();
        var document = new Document(PageSize.A4, 20, 20, 20, 20);
        PdfWriter.GetInstance(document, stream);
        document.Open();

        var baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
        var titleFont = new Font(baseFont, 16, Font.BOLD);
        var normalFont = new Font(baseFont, 12);

        // Tiêu đề
        document.Add(new Paragraph($"Đơn hàng: {order.OrderCode}", titleFont));
        document.Add(new Paragraph($"Ngày đặt: {order.OrderDate:dd/MM/yyyy HH:mm}", normalFont));
        document.Add(new Paragraph($"Người đặt: {order.UserName}", normalFont));
        document.Add(new Paragraph($"Trạng thái: {GetStatusLabel(order.Status)}", normalFont));
        document.Add(new Paragraph(" ")); // dòng trắng

        // Bảng sản phẩm
        var table = new PdfPTable(5) { WidthPercentage = 100 };
        table.SetWidths(new float[] { 5, 30, 15, 10, 15 });

        string[] headers = { "STT", "Tên sản phẩm", "Giá", "Số lượng", "Thành tiền" };
        foreach (var h in headers)
        {
            var cell = new PdfPCell(new Phrase(h, normalFont))
            {
                BackgroundColor = new BaseColor(211, 211, 211),
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            table.AddCell(cell);
        }

        int stt = 1;
        foreach (var item in order.OrderDetails)
        {
            table.AddCell(new PdfPCell(new Phrase(stt++.ToString(), normalFont))
                { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(item.Product?.Name ?? "", normalFont)));
            table.AddCell(new PdfPCell(new Phrase(item.Price.ToString("n0") + " ₫", normalFont))
                { HorizontalAlignment = Element.ALIGN_RIGHT });
            table.AddCell(new PdfPCell(new Phrase(item.Quantity.ToString(), normalFont))
                { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase((item.Price * item.Quantity).ToString("n0") + " ₫", normalFont))
                { HorizontalAlignment = Element.ALIGN_RIGHT });
        }

        document.Add(table);

        // Tổng tiền
        decimal total = order.OrderDetails.Sum(od => od.Price * od.Quantity);
        document.Add(new Paragraph(" "));
        var totalParagraph = new Paragraph($"Tổng tiền: {total.ToString("n0")} ₫", new Font(baseFont, 14, Font.BOLD));
        totalParagraph.Alignment = Element.ALIGN_RIGHT;
        document.Add(totalParagraph);

        document.Close();
        stream.Position = 0;

        return File(stream.ToArray(), "application/pdf", $"{order.OrderCode}_Invoice.pdf");
    }

    private static string GetStatusLabel(int status) => status switch
    {
        1 => "Chờ xác nhận",
        2 => "Đã xác nhận",
        3 => "Đã giao",
        _ => "Đã hủy"
    };
    
}