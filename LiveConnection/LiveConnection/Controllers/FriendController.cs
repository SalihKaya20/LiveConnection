using LiveConnection.Data;
using LiveConnection.DTO;
using LiveConnection.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LiveConnection.Controllers
{   
    
    [Route("api/friends")]
    [ApiController]
    [Authorize]
    public class FriendController : ControllerBase
    {
        private readonly DataContext _context;

        public FriendController(DataContext context)
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


        




        [HttpGet("GetFriends")]
        public async Task<IActionResult> GetFriends()
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
            

            var friends = await _context.Friends
                .Where(f => (f.UserId == userId || f.FriendId == userId) && f.IsAccepted)
                .Include(f => f.User)  
                .Include(f => f.FriendUser)
                .Select(f => new UserDTO
                {
                    Id = f.UserId == userId ? f.FriendUser.Id : f.User.Id,  
                    Username = f.UserId == userId ? f.FriendUser.Username : f.User.Username,  
                    Email = f.UserId == userId ? f.FriendUser.Email : f.User.Email
                })
                .ToListAsync();


            if (!friends.Any())
            {
                return NotFound("Kullanıcının arkadaşı bulunamadı.");
            }

            return Ok(friends);
        }










        [HttpPost("SendFriendRequest")]
        public async Task<IActionResult> SendFriendRequest([FromBody] AddFriendDTO model)
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


            if (userId == model.FriendId)
            {
                return BadRequest("Kendinize arkadaşlık isteği gönderemezsiniz.");
            }



            var friend = await _context.Users.FindAsync(model.FriendId);

            if (friend == null)
            {
                return NotFound("Arkadaş bulunamadı.");
            }


            var existingRequest = await _context.Friends.FirstOrDefaultAsync(f =>
                (f.UserId == userId && f.FriendId == model.FriendId) ||
                (f.UserId == model.FriendId && f.FriendId == userId));

            if (existingRequest != null)
            {
                return BadRequest("Bu kişiyle zaten arkadaşsınız veya isteğiniz beklemede.");
            }

            var friendRequest = new Friend
            {
                UserId = userId,
                FriendId = model.FriendId,
                IsAccepted = false 
            };

            _context.Friends.Add(friendRequest);
            await _context.SaveChangesAsync();

            return Ok("Arkadaşlık isteği gönderildi.");
        }








        
        [HttpPost("AcceptFriendRequest")]
        public async Task<IActionResult> AcceptFriendRequest([FromBody] AcceptFriendDTO model)
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

            var friendRequest = await _context.Friends.FirstOrDefaultAsync(f =>
                f.UserId == model.FriendId && f.FriendId == userId && !f.IsAccepted);

            if (friendRequest == null)
            {
                return NotFound("Arkadaşlık isteği bulunamadı.");
            }


            friendRequest.IsAccepted = true;
            await _context.SaveChangesAsync();

            return Ok("Arkadaşlık isteği kabul edildi.");
        }








        [HttpDelete("RejectFriendRequest")]
        public async Task<IActionResult> RejectFriendRequest([FromBody] RejectFriendRequestDTO model)
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

            if (userId == 0)
            {
                return Unauthorized("Geçersiz işlem.");
            }
            
            var friendRequest = await _context.Friends.FirstOrDefaultAsync(f =>
                f.UserId == model.FriendId && f.FriendId == userId && !f.IsAccepted);


            if (friendRequest == null)
            {
                return NotFound("Arkadaşlık isteği bulunamadı");
            }

            // İsteği reddetmek için kaydı sil
            _context.Friends.Remove(friendRequest);
            await _context.SaveChangesAsync();

            return Ok("Arkadaşlık isteği reddedildi.");
        }








        
        [HttpDelete("RemoveFriend")]
        public async Task<IActionResult> RemoveFriend([FromBody] RemoveFriendDTO model)
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


            if (userId == 0)
            {
                return Unauthorized("Geçersiz işlem.");
            }

            var friendship = await _context.Friends.FirstOrDefaultAsync(f =>
                (f.UserId == userId && f.FriendId == model.FriendId) ||
                (f.UserId == model.FriendId && f.FriendId == userId));

            if (friendship == null)
            {
                return NotFound("Arkadaşlık bulunamadı.");
            }

            _context.Friends.Remove(friendship);
            await _context.SaveChangesAsync();

            return Ok("Arkadaş başarıyla silindi.");
        }










        [HttpGet("PendingRequests/")]
        public async Task<IActionResult> GetPendingRequests()
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


            if (userId == 0)
            {
                return Unauthorized("Geçersiz token.");
            }

            var pendingRequests = await _context.Friends
                .Where(f => f.FriendId == userId && !f.IsAccepted)
                .Include(f => f.User)
                .Select(f => new UserDTO
                {
                    Id = f.User.Id,
                    Username = f.User.Username,
                    Email = f.User.Email
                })
                .ToListAsync();

            if (!pendingRequests.Any())
            {
                return NotFound("Bekleyen arkadaşlık isteğiniz yok.");
            }

            return Ok(pendingRequests);
        }


        
    }
}
