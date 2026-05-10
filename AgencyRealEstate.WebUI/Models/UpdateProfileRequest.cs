namespace AgencyRealEstate.WebUI.Models;

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