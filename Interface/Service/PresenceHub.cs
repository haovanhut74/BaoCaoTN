using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace MyWebApp.Interface.Service
{
    public class PresenceHub : Hub
    {
        // Lưu danh sách user online theo UserName
        private static ConcurrentDictionary<string, string> OnlineUsers = new();

        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.Identity?.Name ?? Context.ConnectionId;
            OnlineUsers[Context.ConnectionId] = userName;

            // Thông báo tất cả client có user mới online
            await Clients.All.SendAsync("UserOnline", userName);

            // Gửi danh sách online hiện tại cho tab mới (Caller)
            var onlineList = OnlineUsers.Values.Distinct().ToList();
            await Clients.Caller.SendAsync("CurrentOnlineUsers", onlineList);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (OnlineUsers.TryRemove(Context.ConnectionId, out var userName))
            {
                await Clients.All.SendAsync("UserOffline", userName);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public static IEnumerable<string> GetOnlineUsers() => OnlineUsers.Values.Distinct();
    }
}