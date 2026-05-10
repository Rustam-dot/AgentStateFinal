using System.ComponentModel.DataAnnotations;
using AgencyRealEstate.API.Data;
using AgencyRealEstate.API.Data.Models;
using AgencyRealEstate.API.Services;
using AgencyRealEstate.API.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace AgencyRealEstate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // Модель для входа
    public class LoginRequest
    {
        [Required(ErrorMessage = "Логин обязателен")]
        [StringLength(50, MinimumLength = 1)]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(1)]
        public string Password { get; set; } = string.Empty;
    }

    // Модель для регистрации
    public class RegisterRequest
    {
        [Required] public string Login { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string FullName { get; set; } = string.Empty;
        [Required] public string Phone { get; set; } = string.Empty;
        // Роль (опционально). Если не указано, используется Client.
        public byte? RoleId { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Login == request.Login && u.IsActive);

        if (user == null)
            return Unauthorized(new { error = "Пользователь с таким логином не найден или неактивен" });

        if (!PasswordService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized(new { error = "Неверный пароль" });

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role.RoleName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"])),
            signingCredentials: creds);

        return Ok(new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            FullName = user.Login,
            Role = user.Role.RoleName
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _context.Users.AnyAsync(u => u.Login == request.Login))
            return Conflict("Логин уже занят");

        
        byte roleId = (byte)UserRoles.Client; 

        
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Administrator"))
        {
            if (request.RoleId.HasValue)
                roleId = request.RoleId.Value;
        }

        var (hash, salt) = PasswordService.CreatePasswordHash(request.Password);

        var user = new User
        {
            Login = request.Login,
            Email = request.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            RoleId = roleId,
            IsActive = true,
            CreatedByUserId = 14 // временно
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Создаём клиента в любом случае, если роль позволяет
        if (roleId == (byte)UserRoles.Client ||
            roleId == (byte)UserRoles.Administrator ||
            roleId == (byte)UserRoles.Manager ||
            roleId == (byte)UserRoles.Realtor)
        {
            var client = new Client
            {
                FullName = request.FullName,
                Phone = request.Phone,
                Email = request.Email,
                UserId = user.UserId,
                CreatedByUserId = 14
            };
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Регистрация прошла успешно" });
    }
}