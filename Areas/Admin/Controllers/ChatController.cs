using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Areas.Admin.Controllers;

public class ChatController : BaseController
{
    public ChatController(DataContext context) : base(context) { }

    // Trang admin chat (view)
    public IActionResult Index()
    {
        return View();
    }

    // API: Lấy danh sách room (unique roomId) + latest message time
    // API: Admin/Chat/Rooms
    [HttpGet]
    public IActionResult Rooms()
    {
        var rooms = _context.ChatMessages
            .GroupBy(m => m.RoomId)
            .Select(g => new
            {
                Id = g.Key,
                UserName = g.FirstOrDefault(x => x.SenderId != "admin").SenderName ?? "Khách", 
                LastMessage = g.OrderByDescending(x => x.SentAt).First().Message,
                LastAt = g.OrderByDescending(x => x.SentAt).First().SentAt
            })
            .OrderByDescending(r => r.LastAt) // ← thêm dòng này
            .ToList();

        return Json(rooms.Select(r => new
        {
            roomId = r.Id,
            userName = string.IsNullOrEmpty(r.UserName) ? "Khách" : WebUtility.HtmlDecode(r.UserName),
            lastMessage = r.LastMessage,
            lastAt = r.LastAt
        }));
    }


    // API: Lấy history tin nhắn của room
    [HttpGet]
    public async Task<IActionResult> Messages(string roomId, int take = 200)
    {
        if (string.IsNullOrEmpty(roomId)) return BadRequest();
        var msgs = await _context.ChatMessages
            .Where(m => m.RoomId == roomId)
            .OrderBy(m => m.SentAt)
            .Take(take)
            .ToListAsync();
        return Ok(msgs);
    }
}