namespace OgrenciBilgiSistemi.Api.Models
{
    public class User
    {
        public int    Id       { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool   IsAdmin  { get; set; }
        public bool   IsActive { get; set; }
        public int?   UnitId   { get; set; }
    }
}
