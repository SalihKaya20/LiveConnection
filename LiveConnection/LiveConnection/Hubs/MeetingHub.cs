using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using LiveConnection.Data;

namespace LiveConnection.Hubs
{
    public class MeetingHub : Hub
    {
        private readonly DataContext _context;
        
        public MeetingHub(DataContext context)
        {
            _context = context;
        }

        // Kullanıcıların bağlantılarını saklamak için (UserID -> ConnectionID)
        private static readonly ConcurrentDictionary<string, string> _connections = new();

        // Meeting odalarını takip etmek için
        private static readonly ConcurrentDictionary<int, HashSet<string>> _meetingRooms = new();

        // Kullanıcı durumlarını takip etmek için
        private static readonly ConcurrentDictionary<string, UserStatus> _userStatuses = new();

        // Kullanıcı durumlarını saklamak için yeni bir dictionary
        private static readonly ConcurrentDictionary<string, UserStatus> _meetingUserStatuses = new();

        public class UserStatus
        {
            public bool IsMuted { get; set; }
            public bool IsVideoEnabled { get; set; }
            public bool IsScreenSharing { get; set; }
        }


        private int GetUserIdFromToken()
        {
            var user = Context.User;
            
            if (user == null)
            {
                throw new UnauthorizedAccessException("Kullanıcı doğrulanamadı.");
            }

            var userIdClaim = user.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("Yetkisiz erişim.");
        }




        // Kullanıcı bağlandığında çağrılacak metod
        public override async Task OnConnectedAsync()
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            _connections[userId.ToString()] = Context.ConnectionId;
            await Clients.Others.SendAsync("UserJoined", userId);
            await base.OnConnectedAsync();
        }

        // Kullanıcı ayrıldığında çağrılacak metod
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            int userId = GetUserIdFromToken();
            if (userId != 0)
            {
                _connections.TryRemove(userId.ToString(), out _);
                await Clients.Others.SendAsync("UserLeft", userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // ICE candidate gönderme (WebRTC bağlantısı için)
        public async Task SendIceCandidate(string recipientUserId, string candidate)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            if (_connections.TryGetValue(recipientUserId, out string? recipientConnectionId))
            {
                await Clients.Client(recipientConnectionId).SendAsync("ReceiveIceCandidate", userId, candidate);
            }
        }

        // SDP Teklifi Gönderme (WebRTC bağlantısı için)
        public async Task SendOffer(string recipientUserId, string offer)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            if (_connections.TryGetValue(recipientUserId, out string? recipientConnectionId))
            {
                await Clients.Client(recipientConnectionId).SendAsync("ReceiveOffer", userId, offer);
            }
        }

        // SDP Cevabı Gönderme (WebRTC bağlantısı için)
        public async Task SendAnswer(string recipientUserId, string answer)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            if (_connections.TryGetValue(recipientUserId, out string? recipientConnectionId))
            {
                await Clients.Client(recipientConnectionId).SendAsync("ReceiveAnswer", userId, answer);
            }
        }

        // Gerçek zamanlı mesajlaşma
        public async Task SendMessage(string message)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            await Clients.Others.SendAsync("ReceiveMessage", userId, message);
        }

        


        // Meeting'e katılma
        public async Task JoinMeeting(int meetingId)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            // Kullanıcıyı meeting odasına ekle
            _meetingRooms.AddOrUpdate(meetingId,
                new HashSet<string> { userId.ToString() },
                (_, users) => { users.Add(userId.ToString()); return users; });

            // Meeting odasındaki diğer kullanıcılara haber ver
            await Clients.Group($"Meeting_{meetingId}").SendAsync("UserJoinedMeeting", userId);
        }

        // Meeting'den ayrılma
        public async Task LeaveMeeting(int meetingId)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            if (_meetingRooms.TryGetValue(meetingId, out var users))
            {
                users.Remove(userId.ToString());
                await Clients.Group($"Meeting_{meetingId}").SendAsync("UserLeftMeeting", userId);
            }
        }

        // Ekran paylaşımı başlatma
        public async Task StartScreenShare(int meetingId)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            await Clients.Group($"Meeting_{meetingId}").SendAsync("ScreenShareStarted", userId);
        }

        // Ekran paylaşımı durdurma
        public async Task StopScreenShare(int meetingId)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            await Clients.Group($"Meeting_{meetingId}").SendAsync("ScreenShareStopped", userId);
        }

        // Mikrofon durumu değiştirme
        public async Task ToggleMute(int meetingId, bool isMuted)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            _userStatuses.AddOrUpdate(userId.ToString(),
                new UserStatus { IsMuted = isMuted },
                (_, status) => { status.IsMuted = isMuted; return status; });

            await Clients.Group($"Meeting_{meetingId}").SendAsync("UserMuteStatusChanged", userId, isMuted);
        }

        // Kamera durumu değiştirme
        public async Task ToggleVideo(int meetingId, bool isEnabled)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            _userStatuses.AddOrUpdate(userId.ToString(),
                new UserStatus { IsVideoEnabled = isEnabled },
                (_, status) => { status.IsVideoEnabled = isEnabled; return status; });

            await Clients.Group($"Meeting_{meetingId}").SendAsync("UserVideoStatusChanged", userId, isEnabled);
        }

        // Özel mesaj gönderme
        public async Task SendPrivateMessage(int meetingId, string recipientId, string message)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            await Clients.User(recipientId).SendAsync("ReceivePrivateMessage", userId, message);
        }

        // Dosya paylaşımı
        public async Task UploadFile(int meetingId, string fileName, string fileUrl)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            await Clients.Group($"Meeting_{meetingId}").SendAsync("FileUploaded", userId, fileName, fileUrl);
        }

        // Kullanıcıyı susturma
        public async Task MuteUser(int meetingId, string targetUserId)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            // Host kontrolü eklenmeli
            var meeting = await _context.Meetings.FindAsync(meetingId);
            if (meeting?.HostUserId != userId)
            {
                throw new UnauthorizedAccessException("Sadece toplantı sahibi kullanıcıları susturabilir.");
            }

            await Clients.User(targetUserId).SendAsync("ForceMute");
        }

        // Kullanıcıyı toplantıdan çıkarma
        public async Task RemoveUser(int meetingId, string targetUserId)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            await Clients.User(targetUserId).SendAsync("RemovedFromMeeting");
        }

        // Kullanıcı durumunu güncelleme
        public async Task UpdateUserStatus(int meetingId, UserStatus status)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            var key = $"{meetingId}_{userId}";
            _meetingUserStatuses.AddOrUpdate(key, status, (_, _) => status);

            await Clients.Group($"Meeting_{meetingId}").SendAsync("UserStatusUpdated", userId, status);
        }

        // Kullanıcı durumlarını getirme
        public async Task GetUserStatuses(int meetingId)
        {
            int userId = GetUserIdFromToken();
            if (userId == 0) return;

            var statuses = _meetingUserStatuses
                .Where(kvp => kvp.Key.StartsWith($"{meetingId}_"))
                .ToDictionary(kvp => int.Parse(kvp.Key.Split('_')[1]), kvp => kvp.Value);

            await Clients.Caller.SendAsync("UserStatusesReceived", statuses);
        }
    }
}
