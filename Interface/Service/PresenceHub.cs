using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Web.Mvc;

namespace MyWebApp.Interface.Service
{
    [Authorize] // đảm bảo chỉ user đã đăng nhập mới connect
    public class PresenceHub : Hub
    {
        // Lưu danh sách online theo UserId → set ConnectionId (multi-tab)
        private static ConcurrentDictionary<string, HashSet<string>> OnlineUsers = new();

        public override async Task OnConnectedAsync()
        {
            // Lấy UserId từ Claims
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Thêm ConnectionId vào danh sách UserId
            if (userId != null)
            {
                OnlineUsers.AddOrUpdate(userId,
                    _ => [Context.ConnectionId],
                    (_, set) =>
                    {
                        set.Add(Context.ConnectionId);
                        return set;
                    });

                // Gửi tới tất cả client: user online
                await Clients.All.SendAsync("UserOnline", userId);
            }

            // Gửi danh sách online cho caller
            await Clients.Caller.SendAsync("CurrentOnlineUsers", OnlineUsers.Keys.ToList());

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId != null && OnlineUsers.TryGetValue(userId, out var set))
            {
                set.Remove(Context.ConnectionId);

                if (!set.Any())
                {
                    // User thực sự offline → remove
                    OnlineUsers.TryRemove(userId, out _);
                    await Clients.All.SendAsync("UserOffline", userId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Lấy danh sách online hiện tại
        public static IEnumerable<string> GetOnlineUsers() => OnlineUsers.Keys;
    }
}