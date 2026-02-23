namespace OgrenciBilgiSistemi.Api.Models
{
    public class Unit
    {
        public int    Id       { get; set; }
        public string Name     { get; set; } = string.Empty;
        public bool   IsActive { get; set; }
        public bool   IsClass  { get; set; }
    }
}
