using LiveConnection.Data;
using LiveConnection.DTO;
using LiveConnection.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LiveConnection.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly DataContext _context;

        public MessageController(DataContext context)
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





        
                       
        [HttpGet("GetMessages")]
        [Authorize]
        public async Task<IActionResult> GetMessages(int? friendId, int? meetingId)
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


            if (meetingId != null && friendId != null)
            {
                return BadRequest("Hem meetingId hem friendId aynı anda kullanılamaz.");
            }

            if (meetingId != null)
            {

                var isParticipant = await _context.Meetings
                .Include(m => m.Participants) 
                .AnyAsync(m => m.Id == meetingId && m.Participants.Any(p => p.UserId == userId));

                if (!isParticipant)
                {
                    return Forbid("Bu toplantının bir üyesi değilsiniz. Mesajları görüntüleyemezsiniz.");
                }


                var messages = await _context.Messages
                    .Where(m => m.MeetingId == meetingId)
                    .OrderBy(m => m.SentAt)
                    .Select(m => new MessageDTO
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        ReceiverId = m.ReceiverId, 
                        Content = m.Content,
                        SentAt = m.SentAt
                    })
                    .ToListAsync();

                return messages.Any() ? Ok(messages) : NotFound("Bu toplantıda mesaj bulunamadı.");
            }

            if (friendId != null)
            {
                var isFriend = await _context.Friends.AnyAsync(f =>
                ((f.UserId == userId && f.FriendId == friendId) ||
                (f.UserId == friendId && f.FriendId == userId)) && f.IsAccepted);

                if (!isFriend)
                return Forbid("Sadece arkadaşlar arasındaki mesajları görüntüleyebilirsiniz.");
            
                var messages = await _context.Messages
                    .Where(m => (m.SenderId == userId && m.ReceiverId == friendId) ||
                                (m.SenderId == friendId && m.ReceiverId == userId))
                    .OrderBy(m => m.SentAt)
                    .Select(m => new MessageDTO
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        ReceiverId = m.ReceiverId, 
                        Content = m.Content,
                        SentAt = m.SentAt
                    })
                    .ToListAsync();

                return messages.Any() ? Ok(messages) : NotFound("İki kullanıcı arasında mesaj bulunamadı.");
            }


            return BadRequest("Geçersiz istek. Ya friendId ya da meetingId belirtilmelidir.");

        }







        [HttpPost("SendMessage")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDTO model)
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

            
            if (userId == model.ReceiverId)
            {
                return BadRequest("Kendine mesaj gönderemezsin.");
            }

            var sender = await _context.Users.FindAsync(userId);
            if (sender == null) return NotFound("Gönderen kullanıcı bulunamadı.");


            if (model.MeetingId != null && model.ReceiverId != null)
            {
                return BadRequest("Aynı anda hem bireysel hem toplantı mesajı gönderilemez.");
            }

            if (model.MeetingId != null)
            {
                var meeting = await _context.Meetings
                    .Include(m => m.Participants)
                    .FirstOrDefaultAsync(m => m.Id == model.MeetingId);

                if (meeting == null) return NotFound("Toplantı bulunamadı.");

                bool isParticipant = meeting.Participants.Any(p => p.UserId == userId);
                if (!isParticipant)
                {
                    return Forbid("Bu toplantının bir üyesi değilsiniz. Mesaj gönderemezsiniz.");
                }

                var messages = meeting.Participants
                    .Where(p => p.UserId != userId) // Kendisine mesaj atmasın
                    .Select(participant => new Message
                    {
                        SenderId = userId,
                        ReceiverId = participant.UserId,
                        MeetingId = model.MeetingId,
                        Content = model.Content,
                        SentAt = DateTime.UtcNow 
                    }).ToList();

                _context.Messages.AddRange(messages);
                await _context.SaveChangesAsync();

                return Ok("Mesaj toplantıya başarıyla gönderildi.");
            }


            if (model.ReceiverId != null)
            {
                var receiver = await _context.Users.FindAsync(model.ReceiverId);
                if (receiver == null) return NotFound("Alıcı kullanıcı bulunamadı.");

                var isFriend = await _context.Friends.AnyAsync(f =>
                    ((f.UserId == userId && f.FriendId == model.ReceiverId) ||
                    (f.UserId == model.ReceiverId && f.FriendId == userId)) && f.IsAccepted);

                if (!isFriend) return BadRequest("Sadece arkadaşlar birbirine mesaj gönderebilir.");

                var message = new Message
                {
                    SenderId = userId,
                    ReceiverId = model.ReceiverId,
                    Content = model.Content,
                    SentAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return Ok("Mesaj başarıyla gönderildi.");
            }

            return BadRequest("Geçersiz mesaj isteği. Alıcı veya toplantı belirtilmelidir.");
        }









        [HttpDelete("DeleteMessage/{messageId}")]
        [Authorize]
        public async Task<IActionResult> DeleteMessage(int messageId)
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

            var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null) return NotFound("Mesaj bulunamadı.");

            if (message.SenderId != userId)
            {
                return Forbid(); 
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();


            return Ok("Mesaj başarıyla silindi.");
        }








        [HttpPost("SaveRealTimeMessage")]
        [Authorize]
        public async Task<IActionResult> SaveRealTimeMessage([FromBody] SaveRealTimeMessageDTO model)
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

            var message = new Message
            {
                SenderId = userId,
                MeetingId = model.MeetingId,
                Content = model.Content,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(message);
        }










        



    }
}
