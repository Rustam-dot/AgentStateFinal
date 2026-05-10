using AgencyRealEstate.API.Data;
using AgencyRealEstate.API.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgencyRealEstate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize(Roles = "Administrator,Manager,Realtor")]
public class ImagesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ImagesController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    /// <summary>
    /// Загрузка фотографий для объекта недвижимости.
    /// Вызывается из Blazor через HttpClient.
    /// </summary>
    /// <param name="propertyId">ID объекта</param>
    /// <param name="files">Список файлов (ключ формы "files")</param>
    [HttpPost("upload/{propertyId}")]
    public async Task<IActionResult> Upload(int propertyId, List<IFormFile> files)
    {
        // Проверяем существование объекта
        var property = await _context.Properties.FindAsync(propertyId);
        if (property == null)
            return NotFound("Объект не найден");

        if (files == null || files.Count == 0)
            return BadRequest("Не выбраны файлы");

        // Папка для сохранения
        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // Получаем ID текущего пользователя для записи в БД
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized();
        int userId = int.Parse(userIdClaim);

        // Проверка, есть ли уже главное фото для этого объекта
        bool hasMainPhoto = _context.PropertyPhotos.Any(p => p.PropertyId == propertyId);

        foreach (var file in files)
        {
            if (file.Length == 0)
                continue;

            // Уникальное имя файла, чтобы избежать конфликтов
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Сохраняем файл на диск
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Запись в БД
            var photo = new PropertyPhoto
            {
                PropertyId = propertyId,
                PhotoUrl = fileName,
                IsMain = !hasMainPhoto, // Первое фото становится главным, если ещё нет
                UploadedByUserId = userId,
                UploadDate = DateTime.UtcNow
            };

            _context.PropertyPhotos.Add(photo);
            hasMainPhoto = true; // Следующие уже не главные
        }

        await _context.SaveChangesAsync();
        return Ok(new { uploaded = files.Count });
    }
}