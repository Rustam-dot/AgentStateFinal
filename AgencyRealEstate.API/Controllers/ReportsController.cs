using AgencyRealEstate.API.Data;
using AgencyRealEstate.API.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgencyRealEstate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator,Manager")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }
    [HttpGet("realtor/{realtorName}")]
    public async Task<IActionResult> GetRealtorDeals(string realtorName)
    {
        var deals = await _context.Deals
            .Include(d => d.Property)
            .Include(d => d.Buyer)
            .Include(d => d.DealStatus)
            .Where(d => d.Realtor != null && d.Realtor.FullName == realtorName &&
                        (d.DealStatus.StatusName == "Completed" || d.DealStatus.StatusName == "Sold"))
            .Select(d => new
            {
                d.DealId,
                PropertyAddress = d.Property.Address,
                BuyerName = d.Buyer.FullName,
                d.Amount,
                StatusName = d.DealStatus.StatusName
            })
            .ToListAsync();

        return Ok(deals);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Manager,Realtor")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePropertyRequest request)
    {
        var property = await _context.Properties.FindAsync(id);
        if (property == null) return NotFound();

        property.Address = request.Address ?? property.Address;
        property.PropertyTypeId = request.PropertyTypeId ?? property.PropertyTypeId;
        property.TotalArea = request.TotalArea ?? property.TotalArea;
        property.LivingArea = request.LivingArea ?? property.LivingArea;
        property.Floor = request.Floor ?? property.Floor;
        property.TotalFloors = request.TotalFloors ?? property.TotalFloors;
        property.Rooms = request.Rooms ?? property.Rooms;
        property.WallMaterialId = request.WallMaterialId ?? property.WallMaterialId;
        property.Price = request.Price ?? property.Price;
        property.Description = request.Description ?? property.Description;

        await _context.SaveChangesAsync();
        return Ok();
    }

    public class UpdatePropertyRequest
    {
        public string? Address { get; set; }
        public int? PropertyTypeId { get; set; }
        public decimal? TotalArea { get; set; }
        public decimal? LivingArea { get; set; }
        public int? Floor { get; set; }
        public int? TotalFloors { get; set; }
        public int? Rooms { get; set; }
        public int? WallMaterialId { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> GetReport()
    {
        // Всего объектов (не удалённых)
        var propertiesTotal = await _context.Properties.CountAsync(p => !p.IsDeleted);

        // Доступные объекты
        var propertiesAvailable = await _context.Properties
            .CountAsync(p => !p.IsDeleted && p.PropertyStatus.StatusName == "Available");

        // Проданные объекты = уникальные объекты из завершённых сделок
        var propertiesSold = await _context.Deals
            .Where(d => d.DealStatusId == 2)   // 2 = Completed
            .Select(d => d.PropertyId)
            .Distinct()
            .CountAsync();

        // Сданные – по статусу Rented
        var propertiesRented = await _context.Properties
            .CountAsync(p => !p.IsDeleted && p.PropertyStatus.StatusName == "Rented");

        // Список ВСЕХ объектов (для детализации по клику)
        var propertyList = await _context.Properties
            .Include(p => p.PropertyStatus)            // ОБЯЗАТЕЛЬНО
            .Where(p => !p.IsDeleted)
            .Select(p => new
            {
                p.PropertyId,
                p.Address,
                p.Price,
                StatusName = p.PropertyStatus.StatusName
            })
            .ToListAsync();

        // Доход по месяцам (завершённые сделки)
        var monthlyRevenue = await _context.Deals
            .Where(d => d.DealStatusId == 2)
            .GroupBy(d => new { d.DealDate.Year, d.DealDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalAmount = g.Sum(d => d.Amount),
                Count = g.Count()
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        // Рейтинг риелторов
        var realtorStats = await _context.Deals
            .Where(d => d.DealStatusId == 2 && d.Realtor != null)
            .GroupBy(d => d.Realtor.FullName)
            .Select(g => new
            {
                RealtorName = g.Key,
                DealsCount = g.Count(),
                TotalAmount = g.Sum(d => d.Amount)
            })
            .OrderByDescending(g => g.TotalAmount)
            .ToListAsync();

        return Ok(new
        {
            Properties = new
            {
                Total = propertiesTotal,
                Available = propertiesAvailable,
                Sold = propertiesSold,
                Rented = propertiesRented,
                List = propertyList
            },
            MonthlyRevenue = monthlyRevenue.Select(d => new
            {
                Period = $"{d.Year}-{d.Month:D2}",
                d.TotalAmount,
                d.Count
            }),
            Realtors = new
            {
                List = realtorStats
            }
        });
    }
}