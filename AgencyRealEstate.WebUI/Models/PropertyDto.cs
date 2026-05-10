namespace AgencyRealEstate.WebUI.Models;

public class PropertyDto
{
    public int PropertyID { get; set; }
    public string Address { get; set; }
    public string PropertyTypeName { get; set; }
    public decimal? TotalArea { get; set; }
    public decimal? LivingArea { get; set; }
    public int? Floor { get; set; }
    public int? TotalFloors { get; set; }
    public int? Rooms { get; set; }
    public string WallMaterialName { get; set; }
    public decimal? Price { get; set; }
    public string Description { get; set; }
    public string StatusName { get; set; }
    public string? Title { get; set; }
    public int? Bathrooms { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<string> PhotoUrls { get; set; }
}