using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LiveConnection.Data;
using LiveConnection.DTO;
using LiveConnection.Entity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IO;

namespace LiveConnection.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MeetingController : ControllerBase
    {
        private readonly DataContext _context;

        public MeetingController(DataContext context)
        {
            _context = context;
        }




        private int GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("Yetkisiz erişim.");
        }







        [HttpPost("create")]
        public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingDTO model)
        {
            int userId;
    
            try
            {
                userId = GetUserIdFromToken();
            }

            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var meeting = new Meeting
            {
                HostUserId = userId,
                ScheduledTime = model.ScheduledTime,
                Title = model.Title,
                CreatedAt = DateTime.UtcNow,
                IsActive = true  
            };

            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Görüşme başarıyla oluşturuldu!", meeting.Id });
        }











         
        [HttpPut("end/{id}")]
        public async Task<IActionResult> EndMeeting(int id)
        {
            int userId;

            try
            {
                userId = GetUserIdFromToken();
            }

            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }


            var meeting = await _context.Meetings.FindAsync(id);

            if (meeting == null)
            {
                return NotFound("Görüşme bulunamadı.");
            }


            if (meeting.HostUserId != userId)
            {
                return Unauthorized("Sadece görüşmeyi başlatan kişi sonlandırabilir.");
            }


            meeting.IsActive = false; 
            await _context.SaveChangesAsync();


            return Ok("Görüşme başarıyla sonlandırıldı.");
        }
        










        [HttpPost("invite")]
        public async Task<IActionResult> InviteFriends([FromBody] InviteFriendsDTO model)
        {
            int userId;

            try
            {
                userId = GetUserIdFromToken();
            }

            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var meeting = await _context.Meetings.FindAsync(model.MeetingId);
            if (meeting == null || !meeting.IsActive)
            {
                return NotFound("Görüşme bulunamadı veya aktif değil.");
            }

            foreach (var friendId in model.FriendIds)
            {
                var invitation = new Invitation
                {
                    MeetingId = model.MeetingId,
                    InvitedUserId = friendId
                };
                _context.Invitations.Add(invitation);
            }

            await _context.SaveChangesAsync();
            return Ok("Arkadaşlar başarıyla davet edildi.");
        }














        [HttpGet("GetInvitations")]
        [Authorize]
        public async Task<IActionResult> GetInvitations()
        {
            int userId;
            try
            {
                userId = GetUserIdFromToken();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var invitations = await _context.Invitations
                .Include(i => i.Meeting)
                .Include(i => i.Sender)
                .Where(i => i.InvitedUserId == userId && i.Status == "Pending")
                .Select(i => new InvitationDTO
                {
                    Id = i.Id,
                    MeetingId = i.MeetingId,
                    MeetingTitle = i.Meeting.Title,
                    InvitedUserId = i.InvitedUserId,
                    InvitedUsername = i.InvitedUser.Username,
                    SenderId = i.SenderId,
                    SenderUsername = i.Sender.Username,
                    Status = i.Status,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            return Ok(invitations);
        }














        [HttpPost("RejectInvitation/{invitationId}")]
        [Authorize]
        public async Task<IActionResult> RejectInvitation(int invitationId)
        {
            int userId;
            try
            {
                userId = GetUserIdFromToken();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var invitation = await _context.Invitations
                .FirstOrDefaultAsync(i => i.Id == invitationId && i.InvitedUserId == userId);

            if (invitation == null)
                return NotFound("Davet bulunamadı.");

            if (invitation.Status != "Pending")
                return BadRequest("Bu davet zaten yanıtlanmış.");

            invitation.Status = "Rejected";
            await _context.SaveChangesAsync();

            return Ok("Davet reddedildi.");
        }












        [HttpGet("CheckInvitationStatus/{meetingId}")]
        [Authorize]
        public async Task<IActionResult> CheckInvitationStatus(int meetingId)
        {
            int userId;
            try
            {
                userId = GetUserIdFromToken();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var invitation = await _context.Invitations
                .FirstOrDefaultAsync(i => i.MeetingId == meetingId && i.InvitedUserId == userId);

            if (invitation == null)
                return NotFound("Bu toplantı için davet bulunamadı.");

            return Ok(new { Status = invitation.Status });
        }













        [HttpPost("join")]
        public async Task<IActionResult> JoinMeeting([FromBody] JoinMeetingDTO model)
        {
            int userId;

            try
            {
                userId = GetUserIdFromToken();
            }

            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var meeting = await _context.Meetings
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == model.MeetingId);

            if (meeting == null)
            {
                return NotFound("Görüşme bulunamadı.");
            }

            var invitation = await _context.Invitations
                .FirstOrDefaultAsync(i => i.MeetingId == model.MeetingId && i.InvitedUserId == userId);

            if (invitation == null)
            {
                return BadRequest("Bu görüşmeye davet edilmediniz.");
            }


             var alreadyParticipant = await _context.Participants
                .AnyAsync(p => p.MeetingId == model.MeetingId && p.UserId == userId);

            if (alreadyParticipant)
            {
                return BadRequest("Zaten bu toplantıya katıldınız.");
            }


            var participant = new Participant
            {
                UserId = userId,
                MeetingId = model.MeetingId
            };

            _context.Participants.Add(participant);
            _context.Invitations.Remove(invitation); 

            await _context.SaveChangesAsync();
            return Ok("Görüşmeye başarıyla katıldınız.");
        }













        [HttpDelete("leave/{meetingId}")]
        public async Task<IActionResult> LeaveMeeting(int meetingId)
        {
            int userId;

            try
            {
                userId = GetUserIdFromToken();
            }
            
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var participant = await _context.Participants
            .FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId == userId);

            if (participant == null)
            {
                return NotFound("Bu görüşmeye katılmıyorsunuz.");
            }

            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();

            return Ok("Görüşmeden başarıyla ayrıldınız.");

        }













        [HttpPost("{meetingId}/screen-share")]
        public async Task<IActionResult> StartScreenShare(int meetingId)
        {
            int userId;
            try
            {
                userId = GetUserIdFromToken();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var meeting = await _context.Meetings.FindAsync(meetingId);
            if (meeting == null) return NotFound("Görüşme bulunamadı.");

            // Kullanıcının bu meeting'e katılıp katılmadığını kontrol et
            var isParticipant = await _context.Participants
                .AnyAsync(p => p.MeetingId == meetingId && p.UserId == userId);


            if (!isParticipant)
            {
                return BadRequest("Bu görüşmeye katılmıyorsunuz.");
            }

            meeting.IsScreenSharingActive = true;
            meeting.ScreenSharingUserId = userId;
            meeting.LastActivityTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Ekran paylaşımı başlatıldı.");
        }











        [HttpPost("{meetingId}/screen-share/stop")]
        public async Task<IActionResult> StopScreenShare(int meetingId)
        {
            int userId;
            try
            {
                userId = GetUserIdFromToken();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var meeting = await _context.Meetings.FindAsync(meetingId);
            if (meeting == null) return NotFound("Görüşme bulunamadı.");

            // Sadece ekran paylaşımını başlatan kişi durdurabilir
            if (meeting.ScreenSharingUserId != userId)
            {
                return Unauthorized("Ekran paylaşımını sadece başlatan kişi durdurabilir.");
            }


            meeting.IsScreenSharingActive = false;
            meeting.ScreenSharingUserId = null;
            meeting.LastActivityTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Ekran paylaşımı durduruldu.");
        }














        [HttpPost("{meetingId}/upload-file")]
        public async Task<IActionResult> UploadFile(int meetingId, IFormFile file)
        {
            int userId;
            try
            {
                userId = GetUserIdFromToken();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            // Kullanıcının bu meeting'e katılıp katılmadığını kontrol et
            var isParticipant = await _context.Participants
                .AnyAsync(p => p.MeetingId == meetingId && p.UserId == userId);

            if (!isParticipant)
            {
                return BadRequest("Bu görüşmeye katılmıyorsunuz.");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("Dosya seçilmedi.");
            }

            // Dosya boyutu kontrolü (örneğin 100MB)
            if (file.Length > 100 * 1024 * 1024)
            {
                return BadRequest("Dosya boyutu çok büyük. Maksimum 100MB yükleyebilirsiniz.");
            }


            // Dosya tipi kontrolü 
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Desteklenmeyen dosya formatı.");
            }


            // Dosya adı güvenliği
            var safeFileName = Path.GetRandomFileName() + fileExtension;
            
            var meetingFile = new MeetingFile
            {
                MeetingId = meetingId,
                UserId = userId,
                FileName = safeFileName,
                FileUrl = "uploaded_file_url", // Dosya yükleme servisi entegrasyonu
                UploadTime = DateTime.UtcNow,
                FileSize = file.Length
            };
            

            _context.MeetingFiles.Add(meetingFile);
            await _context.SaveChangesAsync();
            return Ok(meetingFile);
        }













        [HttpPost("{meetingId}/mute-user")]
        public async Task<IActionResult> MuteUser(int meetingId, string targetUserId)
        {
            int userId;
            try
            {
                userId = GetUserIdFromToken();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var meeting = await _context.Meetings
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null)
            {
                return NotFound("Görüşme bulunamadı.");
            }

            // Sadece toplantı sahibi veya moderatör kullanıcıları susturabilir
            if (meeting.HostUserId != userId)
            {
                return Unauthorized("Sadece toplantı sahibi kullanıcıları susturabilir.");
            }
            

            // Hedef kullanıcının toplantıya katılıp katılmadığını kontrol et
            var isParticipant = await _context.Participants
                .AnyAsync(p => p.MeetingId == meetingId && p.UserId.ToString() == targetUserId);

            if (!isParticipant)
            {
                return BadRequest("Bu kullanıcı toplantıya katılmıyor.");
            }

            return Ok("Kullanıcı susturuldu.");
        }













        [HttpPost("{meetingId}/remove-user")]
        public async Task<IActionResult> RemoveUser(int meetingId, string targetUserId)
        {
            int userId;
            try
            {
                userId = GetUserIdFromToken();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }


            var meeting = await _context.Meetings
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null)
            {
                return NotFound("Görüşme bulunamadı.");
            }

            // Sadece toplantı sahibi kullanıcıları çıkarabilir
            if (meeting.HostUserId != userId)
            {
                return Unauthorized("Sadece toplantı sahibi kullanıcıları çıkarabilir.");
            }

            // Hedef kullanıcının toplantıya katılıp katılmadığını kontrol et
            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.MeetingId == meetingId && p.UserId.ToString() == targetUserId);

            if (participant == null)
            {
                return BadRequest("Bu kullanıcı toplantıya katılmıyor.");
            }


            // Kullanıcıyı toplantıdan çıkar
            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();

            return Ok("Kullanıcı toplantıdan çıkarıldı.");
        }













        [HttpGet("{meetingId}/files")]
        public async Task<IActionResult> GetMeetingFiles(int meetingId)
        {
            int userId;
            try
            {
                userId = GetUserIdFromToken();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var isParticipant = await _context.Participants
                .AnyAsync(p => p.MeetingId == meetingId && p.UserId == userId);


            if (!isParticipant)
            {
                return BadRequest("Bu görüşmeye katılmıyorsunuz.");
            }

            var files = await _context.MeetingFiles
                .Where(f => f.MeetingId == meetingId)
                .OrderByDescending(f => f.UploadTime)
                .ToListAsync();

            return Ok(files);
            
        }











        [HttpGet("{meetingId}/user-statuses")]
        public async Task<IActionResult> GetUserStatuses(int meetingId)
        {
            int userId;
            try
            {
                userId = GetUserIdFromToken();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var isParticipant = await _context.Participants
                .AnyAsync(p => p.MeetingId == meetingId && p.UserId == userId);

            if (!isParticipant)
            {
                return BadRequest("Bu görüşmeye katılmıyorsunuz.");
            }

            var participants = await _context.Participants
                .Where(p => p.MeetingId == meetingId)
                .Include(p => p.User)
                .ToListAsync();

            var statuses = participants.Select(p => new
            {
                UserId = p.UserId,
                Username = p.User.Username,
                IsMuted = false, 
                IsVideoEnabled = true, 
                IsScreenSharing = false 
            });

            return Ok(statuses);
        }








        
    }
}
