using LiveConnection.Data;
using LiveConnection.Entity;
using LiveConnection.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;

namespace LiveConnection.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {

        private readonly IConfiguration _config;
        private readonly DataContext _context ;

        public UserController(DataContext context, IConfiguration config)
        {
            _context = context;
            _config = config;

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




    

        [HttpPost("register")]
        public async Task<IActionResult> Register ([FromBody] RegisterUserDTO model)
        {
            if(await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return BadRequest("Bu e-posta zaten kayıtlı.");
            }

            if (model.Password != model.ConfirmPassword)
            {
                return BadRequest("Şifreler uyuşmuyor.");
            }

            var user = new User
            {
                Username = model.UserName,
                Email = model.Email,
                PasswordHash = HashPassword(model.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Kullanıcı başarıyla oluşturuldu.");

        }






        [HttpPost("login")]
        public async Task<IActionResult> Login ([FromBody] LoginUserDTO model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return Unauthorized("Geçersiz E-posta veya şifre.");
            }

            var token = GenerateJwtToken(user);

            return Ok(new 
            {
                
            message = "Giriş Başarılı.", 
            token,
            user = new UserDTO 
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email}});
            
        }

        
        

        


        [HttpGet("UserList")]
        [Authorize]
        public async Task<IActionResult> UserList()
        {
            var users = await _context.Users
                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email
            })
            .ToListAsync();


            if(!users.Any())
            {
                return NotFound("Kullanıcı Bulunamadı");
            }

            return Ok(users);
        }
        





        

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }






        

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserId", user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationMinutes"]!)),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }







        [HttpPut("UpdateStatus")]
        [Authorize]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateUserStatusDTO model)
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

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            user.Status = model.Status;
            user.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok("Kullanıcı durumu güncellendi.");
        }






        private bool VerifyPassword(string inputPassword, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(inputPassword, hashedPassword);
        }







        [HttpPut("UpdatePassword")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDTO model)
        {
            int userId;
            
            try
            {
                userId = GetUserIdFromToken(); // Kullanıcıyı JWT'den al
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            // Kullanıcının mevcut şifresini kontrol et
            if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("Mevcut şifre hatalı.");
            }

            // Yeni şifreyi kaydet
            user.PasswordHash = HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            return Ok("Şifre başarıyla güncellendi.");
        }


        
    }

}