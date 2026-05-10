namespace AgencyRealEstate.WebUI.Models;


public class CreateShowingRequest
{
    public int PropertyId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan? Time { get; set; }
    public string ClientName { get; set; } = "";
    public string ClientPhone { get; set; } = "";
    public string? Email { get; set; }
    public string? Comments { get; set; }
}