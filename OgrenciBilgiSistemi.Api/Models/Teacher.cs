namespace OgrenciBilgiSistemi.Api.Models
{
    public class Teacher
    {
        public int     Id        { get; set; }
        public string  FullName  { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public bool    IsActive  { get; set; }
        public int?    UnitId    { get; set; }
    }
}
