using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AgencyRealEstate.API.Data;
using AgencyRealEstate.API.Data.Models;
using AgencyRealEstate.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgencyRealEstate.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Administrator")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }


    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var users = await _context.Users
            .Include(u => u.Role)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Login = u.Login,
                Email = u.Email,
                AvatarUrl = !string.IsNullOrEmpty(u.AvatarUrl)
                    ? (u.AvatarUrl.StartsWith("/") ? $"{baseUrl}{u.AvatarUrl}" : u.AvatarUrl)
                    : null,
                Role = u.Role.RoleName,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] AddUserRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Login == request.Login))
            return Conflict("Логин уже занят");

        var (hash, salt) = PasswordService.CreatePasswordHash(request.Password);

        var user = new User
        {
            Login = request.Login,
            Email = request.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            RoleId = (byte)(request.RoleId ?? 4), // по умолчанию Client
            IsActive = true,
            CreatedByUserId = GetCurrentUserId()
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Создаём связанного клиента
        var client = new Client
        {
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            UserId = user.UserId,
            CreatedByUserId = GetCurrentUserId()
        };
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Пользователь создан" });
    }

    public class AddUserRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public int? RoleId { get; set; }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) throw new UnauthorizedAccessException();
        return int.Parse(userIdClaim);
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound("User not found");

        if (!string.IsNullOrWhiteSpace(request.NewLogin))
            user.Login = request.NewLogin;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var (hash, salt) = PasswordService.CreatePasswordHash(request.NewPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
        }

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

       
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (id == currentUserId && request.IsActive == false)
            return BadRequest("You cannot lock yourself.");

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { user.UserId, user.Login, user.Email, user.IsActive });
    }

    public class UpdateUserRequest
    {
        public string? NewLogin { get; set; }
        public string? NewPassword { get; set; }
        public bool? IsActive { get; set; }
    }
}