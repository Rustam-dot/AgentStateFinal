using System.Security.Claims;
using AgencyRealEstate.API.Data;
using AgencyRealEstate.API.Data.Models;
using AgencyRealEstate.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgencyRealEstate.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ProfileController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET api/profile
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        int userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Для клиентов пытаемся найти запись в Clients
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);

        return Ok(new
        {
            Login = user.Login,
            Email = user.Email,
            FullName = client?.FullName ?? user.Login,
            Phone = client?.Phone ?? "",
            PassportData = client?.PassportData,
            Preferences = client?.Preferences,
            Position = user.Position,
            Bio = user.Bio,
            AvatarUrl = string.IsNullOrEmpty(user.AvatarUrl)
    ? null
    : (user.AvatarUrl.StartsWith("/")
        ? $"{Request.Scheme}://{Request.Host}{user.AvatarUrl}"
        : user.AvatarUrl)
        });
    }

    // PUT api/profile
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        int userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Обновляем логин, если передан и не занят
        if (!string.IsNullOrWhiteSpace(request.Login) && request.Login != user.Login)
        {
            if (await _context.Users.AnyAsync(u => u.Login == request.Login && u.UserId != userId))
                return Conflict("Этот логин уже используется");
            user.Login = request.Login;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
            user.Email = request.Email;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var (hash, salt) = PasswordService.CreatePasswordHash(request.NewPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
        }

        // Дополнительные поля – применяются ко всем ролям
        if (request.Position != null) user.Position = request.Position;
        if (request.Bio != null) user.Bio = request.Bio;

        // Обновление клиента (если есть запись в Clients)
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        if (client != null)
        {
            if (request.FullName != null) client.FullName = request.FullName;
            if (request.Phone != null) client.Phone = request.Phone;
            if (request.PassportData != null) client.PassportData = request.PassportData;
            if (request.Preferences != null) client.Preferences = request.Preferences;
            // Для клиентов также сохраняем должность/описание в Users (уже сделали выше)
        }
        else
        {
            // Если записи клиента ещё нет – создаём для роли Client
            // Для сотрудников таблица Clients не обязательна, поэтому создаём только при необходимости.
            // Чтобы не создавать пустого клиента для риелтора, проверим роль.
            var role = await _context.UserRoles.FindAsync(user.RoleId);
            if (role != null && role.RoleName == "Client")
            {
                client = new Client
                {
                    UserId = userId,
                    FullName = request.FullName ?? user.Login,
                    Phone = request.Phone ?? "",
                    PassportData = request.PassportData,
                    Preferences = request.Preferences,
                    CreatedByUserId = userId
                };
                _context.Clients.Add(client);
            }
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Профиль обновлён" });
    }

    // POST api/profile/avatar – загрузка аватара
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не выбран");

        int userId = GetCurrentUserId();
        var uploadsFolder = Path.Combine(_env.WebRootPath, "avatars");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{userId}_{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var avatarUrl = $"{baseUrl}/avatars/{fileName}";

        user.AvatarUrl = avatarUrl;
        await _context.SaveChangesAsync();

        return Ok(new { avatarUrl });
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) throw new UnauthorizedAccessException();
        return int.Parse(userIdClaim);
    }
}

public class UpdateProfileRequest
{
    public string? Login { get; set; }
    public string? Email { get; set; }
    public string? NewPassword { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? PassportData { get; set; }
    public string? Preferences { get; set; }
    public string? Position { get; set; }
    public string? Bio { get; set; }
}