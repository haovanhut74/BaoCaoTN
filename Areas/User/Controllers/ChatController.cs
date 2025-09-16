using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;
using Controller = System.Web.Mvc.Controller;

namespace MyWebApp.Areas.User.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly DataContext _context;

    public ChatController(DataContext context)
    {
        _context = context;
    }

    // GET: api/chat/history/{roomId}

    [HttpGet("history/{roomId}")]
    public IActionResult GetHistory(string roomId)
    {
        var messages = _context.ChatMessages
            .Where(m => m.RoomId == roomId)
            .OrderBy(m => m.SentAt)
            .ToList();

        return Ok(messages);
    }
}