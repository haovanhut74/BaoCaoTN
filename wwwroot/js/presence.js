const connection = new signalR.HubConnectionBuilder()
    .withUrl("/presenceHub", { withCredentials: true })
    .withAutomaticReconnect()
    .build();

// Nhận danh sách online khi connect
connection.on("CurrentOnlineUsers", function(userIds){
    document.querySelectorAll("[data-user-id]").forEach(td => {
        td.innerHTML = '<span class="badge bg-secondary">Offline</span>';
    });

    userIds.forEach(id => {
        const badge = document.querySelector("[data-user-id='" + id + "']");
        if(badge) badge.innerHTML = '<span class="badge bg-success">Online</span>';
    });
});

// Khi có user online
connection.on("UserOnline", function(userId){
    const badge = document.querySelector("[data-user-id='" + userId + "']");
    if(badge) badge.innerHTML = '<span class="badge bg-success">Online</span>';
});

// Khi user offline
connection.on("UserOffline", function(userId){
    const badge = document.querySelector("[data-user-id='" + userId + "']");
    if(badge) badge.innerHTML = '<span class="badge bg-secondary">Offline</span>';
});

connection.start().catch(err => console.error(err.toString()));
