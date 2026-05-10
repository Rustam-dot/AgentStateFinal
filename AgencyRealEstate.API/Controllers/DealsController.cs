using System.Security.Claims;
using AgencyRealEstate.API.Data;
using AgencyRealEstate.API.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgencyRealEstate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DealsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DealsController(AppDbContext context)
    {
        _context = context;
    }

    // Создание сделки клиентом
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDealRequest request)
    {
        int userId = GetCurrentUserId();
        var showing = await _context.Showings
            .Include(s => s.Realtor)
            .FirstOrDefaultAsync(s => s.ShowingId == request.ShowingId);

        if (showing == null) return NotFound("Показ не найден");

        int realtorId = showing.RealtorId ?? 2;
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        if (client == null) return BadRequest("Клиент не найден");

        var deal = new Deal
        {
            PropertyId = showing.PropertyId,
            SellerId = client.ClientId,
            BuyerId = client.ClientId,
            RealtorId = realtorId,
            DealTypeId = request.DealTypeId,
            DealDate = DateTime.UtcNow,
            Amount = request.Amount,
            DealStatusId = (byte)DealStatus.InProgress,
            CreatedByUserId = userId
        };

        _context.Deals.Add(deal);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Сделка создана", dealId = deal.DealId });
    }

    // Сделки клиента
    [HttpGet("my")]
    public async Task<IActionResult> GetMyDeals()
    {
        int userId = GetCurrentUserId();
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        if (client == null) return BadRequest("Клиент не найден");

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var deals = await _context.Deals
            .Include(d => d.Property)
            .Include(d => d.Realtor)
            .Include(d => d.DealStatus)
            .Include(d => d.DealType)
            .Where(d => d.BuyerId == client.ClientId)
            .Select(d => new
            {
                d.DealId,
                d.DealDate,
                d.Amount,
                PropertyTitle = d.Property.Title,
                PropertyAddress = d.Property.Address,
                PropertyStatus = d.Property.PropertyStatus.StatusName,
                RealtorName = d.Realtor != null ? d.Realtor.FullName : "—",
                StatusName = d.DealStatus.StatusName,
                CanCancel = d.DealStatusId == (byte)DealStatus.InProgress
            })
            .OrderByDescending(d => d.DealDate)
            .ToListAsync();

        return Ok(deals);
    }

   
    [HttpPut("{id}/accept")]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> AcceptDeal(int id)
    {
        var deal = await _context.Deals.Include(d => d.Property).FirstOrDefaultAsync(d => d.DealId == id);
        if (deal == null) return NotFound();

        if (deal.DealStatusId != (byte)DealStatus.AwaitingApproval)
            return BadRequest("Нельзя принять сделку сейчас");

        deal.DealStatusId = (byte)DealStatus.Completed;

        var soldStatusId = await _context.PropertyStatuses
            .Where(s => s.StatusName == "Sold").Select(s => s.PropertyStatusId).FirstOrDefaultAsync();
        var rentedStatusId = await _context.PropertyStatuses
            .Where(s => s.StatusName == "Rented").Select(s => s.PropertyStatusId).FirstOrDefaultAsync();
        deal.Property.PropertyStatusId = (deal.DealTypeId == 1 && soldStatusId != 0) ? soldStatusId : rentedStatusId;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Сделка принята" });
    }


    // Сделки риелтора — ОБНОВЛЕНО С АВТАРАМИ И НАЗВАНИЯМИ
    [HttpGet("assigned")]
    [Authorize(Roles = "Administrator,Manager,Realtor")]
    public async Task<IActionResult> GetAssignedDeals()
    {
        int userId = GetCurrentUserId();
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null) return BadRequest("Сотрудник не найден");

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var deals = await _context.Deals
            .Include(d => d.Property)
            .Include(d => d.Buyer)
                .ThenInclude(b => b.User)  // 👈 Для получения аватара
            .Include(d => d.DealStatus)
            .Include(d => d.DealType)
            .Where(d => d.RealtorId == employee.EmployeeId)
            .Select(d => new
            {
                d.DealId,
                d.DealDate,
                d.Amount,
                PropertyTitle = d.Property.Title,  // 👈 Название объекта
                PropertyAddress = d.Property.Address,
                BuyerName = d.Buyer.FullName,
                BuyerAvatarUrl = d.Buyer.User != null && !string.IsNullOrEmpty(d.Buyer.User.AvatarUrl)
                    ? (d.Buyer.User.AvatarUrl.StartsWith("/")
                        ? $"{baseUrl}{d.Buyer.User.AvatarUrl}"
                        : d.Buyer.User.AvatarUrl)
                    : null,  // 👈 Аватар покупателя с абсолютным URL
                StatusName = d.DealStatus.StatusName,
                DealTypeName = d.DealType.Name,
                CanComplete = d.DealStatusId == (byte)DealStatus.InProgress || d.DealStatusId == (byte)DealStatus.DocumentPending
            })
            .OrderByDescending(d => d.DealDate)
            .ToListAsync();

        return Ok(deals);
    }


    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Manager,Realtor")]
    public async Task<IActionResult> UpdateDeal(int id, [FromBody] UpdateDealRequest request)
    {
        var deal = await _context.Deals.FindAsync(id);
        if (deal == null) return NotFound("Сделка не найдена");
        if (deal.DealStatusId == (byte)DealStatus.Completed || deal.DealStatusId == (byte)DealStatus.Cancelled)
            return BadRequest("Нельзя редактировать завершённую или отменённую сделку");

        if (request.DealTypeId.HasValue) deal.DealTypeId = request.DealTypeId.Value;
        if (request.Amount.HasValue) deal.Amount = request.Amount.Value;

    
        deal.DealStatusId = (byte)DealStatus.AwaitingApproval;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Сделка обновлена и отправлена клиенту" });
    }

    [HttpPut("{id}/complete")]
    [Authorize(Roles = "Administrator,Manager,Realtor")]
    public async Task<IActionResult> CompleteDeal(int id)
    {
        var deal = await _context.Deals
            .Include(d => d.Property)
            .FirstOrDefaultAsync(d => d.DealId == id);

        if (deal == null) return NotFound("Сделка не найдена");
        if (deal.DealStatusId == (byte)DealStatus.Completed)
            return BadRequest("Сделка уже завершена");

        deal.DealStatusId = (byte)DealStatus.Completed;

        var soldStatusId = await _context.PropertyStatuses
            .Where(s => s.StatusName == "Sold")
            .Select(s => s.PropertyStatusId)
            .FirstOrDefaultAsync();

        var rentedStatusId = await _context.PropertyStatuses
            .Where(s => s.StatusName == "Rented")
            .Select(s => s.PropertyStatusId)
            .FirstOrDefaultAsync();

        deal.Property.PropertyStatusId = (deal.DealTypeId == 1 && soldStatusId != 0) ? soldStatusId : rentedStatusId;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Сделка завершена", newStatus = deal.Property.PropertyStatusId });
    }

   
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Administrator,Manager,Realtor")]
    public async Task<IActionResult> RejectDeal(int id)
    {
        var deal = await _context.Deals
            .Include(d => d.Property)
            .FirstOrDefaultAsync(d => d.DealId == id);
        if (deal == null) return NotFound("Сделка не найдена");

        deal.DealStatusId = (byte)DealStatus.Cancelled;

        var availableId = await _context.PropertyStatuses
            .Where(s => s.StatusName == "Available")
            .Select(s => s.PropertyStatusId)
            .FirstOrDefaultAsync();

        deal.Property.PropertyStatusId = availableId;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Сделка отклонена" });
    }

   
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelDeal(int id)
    {
        var deal = await _context.Deals.Include(d => d.Property).FirstOrDefaultAsync(d => d.DealId == id);
        if (deal == null) return NotFound();

        if (deal.DealStatusId == (byte)DealStatus.Completed)
            return BadRequest("Завершённую сделку отменить нельзя");

        deal.DealStatusId = (byte)DealStatus.Cancelled;

        var availableStatusId = await _context.PropertyStatuses
            .Where(s => s.StatusName == "Available").Select(s => s.PropertyStatusId).FirstOrDefaultAsync();
        deal.Property.PropertyStatusId = availableStatusId;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Сделка отменена" });
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) throw new UnauthorizedAccessException();
        return int.Parse(userIdClaim);
    }
}

public class CreateDealRequest
{
    public int ShowingId { get; set; }
    public int DealTypeId { get; set; }
    public decimal Amount { get; set; }
}

public class UpdateDealRequest
{
    public int? DealTypeId { get; set; }
    public decimal? Amount { get; set; }
    public string? Comments { get; set; }
}

public enum DealStatus
{
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
    DocumentPending = 4,
    AwaitingApproval = 5
}