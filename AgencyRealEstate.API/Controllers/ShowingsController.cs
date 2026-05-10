using AgencyRealEstate.API.Data;
using AgencyRealEstate.API.Data.Models;
using AgencyRealEstate.API.DTOs;
using AgencyRealEstate.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgencyRealEstate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShowingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ShowingsController(AppDbContext context)
    {
        _context = context;
    }

    // Создание заявки на показ
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShowingRequest request)
    {
        int currentUserId = GetCurrentUserId();
        string normalizedPhone = NormalizePhone(request.ClientPhone);

        var client = await _context.Clients
            .FirstOrDefaultAsync(c =>
                c.Phone != null &&
                c.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "") == normalizedPhone);

        if (client == null)
        {
            client = new Client
            {
                FullName = request.ClientName,
                Phone = request.ClientPhone,
                Email = request.Email,                   // 👈 сохраняем email
                CreatedByUserId = currentUserId
            };
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.ClientName))
                client.FullName = request.ClientName;
            if (!string.IsNullOrWhiteSpace(request.Email))  // 👈 обновляем email, если передан
                client.Email = request.Email;
        }

        var showing = new Showing
        {
            PropertyId = request.PropertyId,
            ClientId = client.ClientId,
            ShowingDateTime = request.Date.Add(request.Time ?? TimeSpan.Zero),
            Comments = request.Comments,
            CreatedByUserId = currentUserId
        };

        _context.Showings.Add(showing);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Заявка создана", showingId = showing.ShowingId });
    }

    [HttpGet("all-assigned")]
    [Authorize(Roles = "Administrator,Manager,Realtor")]
    public async Task<IActionResult> GetAllAssignedShowings()
    {
        int userId = GetCurrentUserId();
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null) return BadRequest("Сотрудник не найден");

        var showings = await _context.Showings
            .Include(s => s.Property).ThenInclude(p => p.PropertyType)
            .Include(s => s.Client)
            .Where(s => s.RealtorId == employee.EmployeeId)
            .Select(s => new
            {
                s.ShowingId,
                s.ShowingDateTime,
                PropertyAddress = s.Property.Address,
                PropertyTitle = s.Property.Title,  // 👈 ДОБАВИТЬ ЭТО
                PropertyTypeName = s.Property.PropertyType.Name,
                ClientName = s.Client.FullName,
                ClientPhone = s.Client.Phone,
                ClientEmail = s.Client.Email,
                ClientPassport = s.Client.PassportData,
                RealtorName = s.Realtor != null ? s.Realtor.FullName : null,
                ResultId = s.ShowingResultId,
                s.Comments
            })
            .OrderByDescending(s => s.ShowingDateTime)
            .ToListAsync();

        return Ok(showings);
    }

    // Список свободных показов (RealtorId == null)
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableShowings()
    {
        var showings = await _context.Showings
            .Include(s => s.Property)
            .Include(s => s.Client)
            .Where(s => s.RealtorId == null)
            .Select(s => new
            {
                s.ShowingId,
                s.ShowingDateTime,
                ClientName = s.Client.FullName,
                ClientPhone = s.Client.Phone,
                PropertyAddress = s.Property.Address,
                PropertyTypeName = s.Property.PropertyType.Name,
                s.Comments
            })
            .OrderBy(s => s.ShowingDateTime)
            .ToListAsync();

        return Ok(showings);
    }

    [HttpGet("my")]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> GetMyShowings()
    {
        int userId = GetCurrentUserId();
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        int? clientId = client?.ClientId;

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var showings = await _context.Showings
            .Include(s => s.Property)
            .Include(s => s.Realtor)
                .ThenInclude(r => r.User)  // Получаем пользователя риелтора
            .Include(s => s.ShowingResult)
            .Where(s => s.CreatedByUserId == userId)
            .Select(s => new
            {
                s.ShowingId,
                s.ShowingDateTime,
                PropertyAddress = s.Property.Address,
                RealtorName = s.Realtor != null ? s.Realtor.FullName : "Не назначен",
                RealtorAvatarUrl = s.Realtor != null && s.Realtor.User != null && !string.IsNullOrEmpty(s.Realtor.User.AvatarUrl)
                    ? (s.Realtor.User.AvatarUrl.StartsWith("/")
                        ? $"{baseUrl}{s.Realtor.User.AvatarUrl}"
                        : s.Realtor.User.AvatarUrl)
                    : null,
                ResultName = s.ShowingResult != null ? s.ShowingResult.ResultName : null,
                s.Comments,
                CanDeal = s.ShowingResult != null &&
                    (s.ShowingResult.ResultName == "Completed" || s.ShowingResult.ResultName == "Interested") &&
                    (clientId == null || !_context.Deals.Any(d =>
                        d.PropertyId == s.PropertyId &&
                        d.BuyerId == clientId &&
                        d.DealStatusId != 3))
            })
            .OrderByDescending(s => s.ShowingDateTime)
            .ToListAsync();

        return Ok(showings);
    }

    // Показы, назначенные текущему риелтору
    [HttpGet("assigned")]
    [Authorize(Roles = "Administrator,Manager,Realtor")]
    public async Task<IActionResult> GetAssignedShowings()
    {
        int userId = GetCurrentUserId();
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null) return BadRequest("Сотрудник не найден");

        var showings = await _context.Showings
            .Include(s => s.Property).ThenInclude(p => p.PropertyType)
            .Include(s => s.Client)
            .Where(s => s.RealtorId == employee.EmployeeId && s.ShowingResultId == null)
            .Select(s => new
            {
                s.ShowingId,
                s.ShowingDateTime,
                PropertyAddress = s.Property.Address,
                PropertyTypeName = s.Property.PropertyType.Name,
                ClientName = s.Client.FullName,
                ClientPhone = s.Client.Phone,
                ClientEmail = s.Client.Email,               
                ClientPassport = s.Client.PassportData,     
                s.Comments,
                SelectedResultId = s.ShowingResultId   
            })
            .OrderByDescending(s => s.ShowingDateTime)
            .ToListAsync();

        return Ok(showings);
    }

    // Взять показ в работу (риелтор)
    [HttpPut("{id}/accept")]
    public async Task<IActionResult> AcceptShowing(int id)
    {
        var showing = await _context.Showings.FindAsync(id);
        if (showing == null) return NotFound("Показ не найден");
        if (showing.RealtorId != null) return BadRequest("Показ уже занят другим риелтором");

        int userId = GetCurrentUserId();
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null) return BadRequest("Ваш профиль сотрудника не найден");

        showing.RealtorId = employee.EmployeeId;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Показ взят в работу", showingId = id });
    }

    // Обновить результат показа
    [HttpPut("{id}/result")]
    [Authorize(Roles = "Administrator,Manager,Realtor")]
    public async Task<IActionResult> UpdateShowingResult(int id, [FromBody] UpdateShowingResultRequest request)
    {
        var showing = await _context.Showings.FindAsync(id);
        if (showing == null) return NotFound("Показ не найден");

        int userId = GetCurrentUserId();
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null || showing.RealtorId != employee.EmployeeId)
            return Forbid("Вы не можете менять этот показ");

        showing.ShowingResultId = request.ResultId;
        if (!string.IsNullOrWhiteSpace(request.Comments))
            showing.Comments = request.Comments;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Результат обновлён" });
    }

    // Вспомогательные методы
    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length > 0 && digits[0] == '8')
            digits = "7" + digits.Substring(1);
        return digits;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) throw new UnauthorizedAccessException();
        return int.Parse(userIdClaim);
    }
}

public class UpdateShowingResultRequest
{
    public byte ResultId { get; set; }
    public string? Comments { get; set; }
}