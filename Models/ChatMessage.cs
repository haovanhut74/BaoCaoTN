using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public string RoomId { get; set; } = string.Empty; // ở đây dùng UserId làm roomId

    [Required] public string SenderId { get; set; } = string.Empty; // userId hoặc "admin"

    public string? SenderName { get; set; } // hiển thị tên

    [Required] public string Message { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}