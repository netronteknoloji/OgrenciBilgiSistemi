using System.Text.Json.Serialization;

namespace StudentTrackingSystem.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }
        public int? UnitId { get; set; }
    }
}
