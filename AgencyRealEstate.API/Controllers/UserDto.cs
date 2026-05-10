namespace AgencyRealEstate.API.Controllers
{
    internal class UserDto
    {
        public int UserId { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}