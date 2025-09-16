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
    private static readonly ConcurrentDictionary<string, string> _connections = new();

    public ChatHub(DataContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext();
        var roomId = http?.Request.Query["roomId"].ToString(); // roomId = userId or "admin"
        if (!string.IsNullOrEmpty(roomId))
        {
            // Save mapping
            _connections[roomId] = Context.ConnectionId;
            // Join the group for that room
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // remove mapping(s) that point to this connection
        var item = _connections.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId);
        if (!string.IsNullOrEmpty(item.Key))
        {
            _connections.TryRemove(item.Key, out _);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // User or Admin calls this to join a room (roomId = userId)
    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        _connections[roomId] = Context.ConnectionId;
    }

    // Gửi message trong room -> mọi client join room nhận
    public async Task SendMessageToRoom(string roomId, string senderId, string senderName, string message)
    {
        // Save to DB
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

        // gửi tới group (room)
        await Clients.Group(roomId).SendAsync("ReceiveMessage", new
        {
            roomId,
            senderId,
            senderName,
            message,
            sentAt = chat.SentAt
        });
        // gửi luôn cho admin
        if (roomId != "admin")
        {
            if (_connections.TryGetValue("admin", out var adminConn))
            {
                await Clients.Client(adminConn).SendAsync("ReceiveMessage", new
                {
                    roomId,
                    senderId,
                    senderName,
                    message,
                    sentAt = chat.SentAt
                });
            }
        }
    }

    // Tùy chọn: Admin có thể gửi trực tiếp tới connectionId nếu cần
    public async Task SendDirect(string connectionId, string senderName, string message)
    {
        await Clients.Client(connectionId).SendAsync("ReceiveDirect", senderName, message);
    }
}