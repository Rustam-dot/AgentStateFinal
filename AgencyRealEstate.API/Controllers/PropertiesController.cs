using System.Security.Claims;
using AgencyRealEstate.API.Data;
using AgencyRealEstate.API.Data.Models;
using AgencyRealEstate.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgencyRealEstate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PropertiesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PropertiesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager,Realtor")]
    public async Task<IActionResult> Create([FromBody] CreatePropertyRequest request)
    {
        var property = new Property
        {
            Title = request.Title,
            Address = request.Address,
            PropertyTypeId = request.PropertyTypeId,
            TotalArea = request.TotalArea,
            LivingArea = request.LivingArea,
            Floor = request.Floor,
            TotalFloors = request.TotalFloors,
            Rooms = request.Rooms,
            Bathrooms = request.Bathrooms,
            WallMaterialId = request.WallMaterialId,
            Price = request.Price,
            Description = request.Description,
            PropertyStatusId = 1,   // "Available"
            CreatedByUserId = GetCurrentUserId()
        };

        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        return Ok(new { property.PropertyId });
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<PropertyDto>>> GetAll()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var properties = await _context.Properties
            .Include(p => p.PropertyType)
            .Include(p => p.WallMaterial)
            .Include(p => p.PropertyStatus)
            .Include(p => p.PropertyPhotos)
            .Where(p => !p.IsDeleted
                        && p.PropertyStatus.StatusName != "Sold"
                        && p.PropertyStatus.StatusName != "Rented")
            .Select(p => new PropertyDto
            {
                PropertyID = p.PropertyId,
                Title = p.Title,
                Address = p.Address,
                PropertyTypeName = p.PropertyType.Name,
                TotalArea = p.TotalArea,
                LivingArea = p.LivingArea,
                Floor = p.Floor,
                TotalFloors = p.TotalFloors,
                Rooms = p.Rooms,
                Bathrooms = p.Bathrooms,
                WallMaterialName = p.WallMaterial != null ? p.WallMaterial.Name : null,
                Price = p.Price,
                Description = p.Description,
                StatusName = p.PropertyStatus.StatusName,
                Latitude = (double?)p.Latitude,
                Longitude = (double?)p.Longitude,
                PhotoUrls = p.PropertyPhotos.Select(ph => $"{baseUrl}/uploads/{ph.PhotoUrl}").ToList()
            })
            .ToListAsync();

        return Ok(properties);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Manager,Realtor")]
    public async Task<IActionResult> Update(int id, [FromBody] EditPropertyModel model)
    {
        var property = await _context.Properties.FindAsync(id);
        if (property == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(model.Title)) property.Title = model.Title;
        if (!string.IsNullOrWhiteSpace(model.Address)) property.Address = model.Address;
        if (model.Price.HasValue) property.Price = model.Price;
        if (model.Rooms.HasValue) property.Rooms = model.Rooms;
        if (model.Bathrooms.HasValue) property.Bathrooms = model.Bathrooms;
        if (model.TotalArea.HasValue) property.TotalArea = model.TotalArea;
        if (model.LivingArea.HasValue) property.LivingArea = model.LivingArea;
        if (model.Floor.HasValue) property.Floor = model.Floor;
        if (model.TotalFloors.HasValue) property.TotalFloors = model.TotalFloors;
        if (model.PropertyTypeId.HasValue) property.PropertyTypeId = model.PropertyTypeId.Value;
        if (model.WallMaterialId.HasValue) property.WallMaterialId = model.WallMaterialId;
        if (model.Description != null) property.Description = model.Description;

        property.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var property = await _context.Properties.FindAsync(id);
        if (property == null) return NotFound();

        property.IsDeleted = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Объект удалён" });
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<PropertyDto>> GetById(int id)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var property = await _context.Properties
            .Include(p => p.PropertyType)
            .Include(p => p.WallMaterial)
            .Include(p => p.PropertyStatus)
            .Include(p => p.PropertyPhotos)
            .Where(p => !p.IsDeleted
                        && p.PropertyStatus.StatusName != "Sold"
                        && p.PropertyStatus.StatusName != "Rented")
            .Select(p => new PropertyDto
            {
                PropertyID = p.PropertyId,
                Title = p.Title,
                Address = p.Address,
                PropertyTypeName = p.PropertyType.Name,
                TotalArea = p.TotalArea,
                LivingArea = p.LivingArea,
                Floor = p.Floor,
                TotalFloors = p.TotalFloors,
                Rooms = p.Rooms,
                Bathrooms = p.Bathrooms,
                WallMaterialName = p.WallMaterial != null ? p.WallMaterial.Name : null,
                Price = p.Price,
                Description = p.Description,
                StatusName = p.PropertyStatus.StatusName,
                Latitude = (double?)p.Latitude,
                Longitude = (double?)p.Longitude,
                PhotoUrls = p.PropertyPhotos.Select(ph => $"{baseUrl}/uploads/{ph.PhotoUrl}").ToList()
            })
            .FirstOrDefaultAsync(p => p.PropertyID == id);

        if (property == null)
            return NotFound();

        return Ok(property);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            throw new UnauthorizedAccessException("User ID not found in token");
        return int.Parse(userIdClaim);
    }
}

public class CreatePropertyRequest
{
    public string? Title { get; set; }
    public string Address { get; set; }
    public int PropertyTypeId { get; set; }
    public decimal TotalArea { get; set; }
    public decimal? LivingArea { get; set; }
    public int? Floor { get; set; }
    public int? TotalFloors { get; set; }
    public int? Rooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? WallMaterialId { get; set; }
    public decimal? Price { get; set; }
    public string? Description { get; set; }
}

public class EditPropertyModel
{
    public string? Title { get; set; }
    public string? Address { get; set; }
    public decimal? Price { get; set; }
    public int? Rooms { get; set; }
    public int? Bathrooms { get; set; }
    public decimal? TotalArea { get; set; }
    public decimal? LivingArea { get; set; }
    public int? Floor { get; set; }
    public int? TotalFloors { get; set; }
    public int? PropertyTypeId { get; set; }
    public int? WallMaterialId { get; set; }
    public string? Description { get; set; }
}