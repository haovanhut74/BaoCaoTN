using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Interface.Hubs;

public class ChatHub : Hub
{
    private readonly DataContext _context;

    // Map userId -> connectionId (multi connections per user supported with list if muốn)
    // For simplicity, use ConcurrentDictionary<string, string> storing latest connectionId per user
    private static readonly ConcurrentDictionary<string, HashSet<string>> _connections = new();

    public ChatHub(DataContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext();
        var roomId = http?.Request.Query["roomId"].ToString();
        if (!string.IsNullOrEmpty(roomId))
        {
            _connections.TryAdd(roomId, new HashSet<string>());
            _connections[roomId].Add(Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var roomEntry = _connections.FirstOrDefault(kvp => kvp.Value.Contains(Context.ConnectionId));
        if (!string.IsNullOrEmpty(roomEntry.Key))
        {
            _connections[roomEntry.Key].Remove(Context.ConnectionId);
            if (_connections[roomEntry.Key].Count == 0)
                _connections.TryRemove(roomEntry.Key, out _);
        }

        await base.OnDisconnectedAsync(exception);
    }


    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Tạo HashSet nếu chưa tồn tại
        _connections.TryAdd(roomId, new HashSet<string>());

        // Thêm connectionId vào set
        _connections[roomId].Add(Context.ConnectionId);
    }


    public async Task SendMessageToRoom(string roomId, string senderId, string senderName, string message)
    {
        var chat = new ChatMessage
        {
            RoomId = roomId,
            SenderId = senderId,
            SenderName = senderName,
            Message = message,
            SentAt = DateTime.UtcNow
        };
        _context.ChatMessages.Add(chat);
        await _context.SaveChangesAsync();

        // Gửi tới tất cả trong room (user + admin đã join)
        await Clients.Group(roomId).SendAsync("ReceiveMessage", new
        {
            roomId,
            senderId,
            senderName,
            message,
            sentAt = chat.SentAt
        });

        if (roomId != "admin")
        {
            if (_connections.TryGetValue("admin", out var adminConns))
            {
                foreach (var connId in adminConns)
                {
                    // Kiểm tra connection này có nằm trong room hiện tại không
                    if (!_connections.TryGetValue(roomId, out var roomConns) || !roomConns.Contains(connId))
                    {
                        await Clients.Client(connId).SendAsync("ReceiveMessage",
                            new { roomId, senderId, senderName, message, sentAt = chat.SentAt });
                    }
                }
            }
        }

    }


    // Tùy chọn: Admin có thể gửi trực tiếp tới connectionId nếu cần
    public async Task SendDirect(string connectionId, string senderName, string message)
    {
        await Clients.Client(connectionId).SendAsync("ReceiveDirect", senderName, message);
    }
}