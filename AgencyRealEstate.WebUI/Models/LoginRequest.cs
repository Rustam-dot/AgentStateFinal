using System.ComponentModel.DataAnnotations;

namespace AgencyRealEstate.WebUI.Models;

public class LoginRequest
{
    [Required(ErrorMessage = "Логин обязателен")]
    [StringLength(50, MinimumLength = 1)]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(1)]
    public string Password { get; set; } = "";
}