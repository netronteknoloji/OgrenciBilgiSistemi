namespace OgrenciBilgiSistemi.DTOs
{
    public class MenuOgeDto
    {
        public int Id { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public int? AnaMenuId { get; set; }
        public int Sirala { get; set; }

        public List<MenuOgeDto> AltOgeler { get; set; } = new();

        public bool YaprakMi =>
            !string.IsNullOrWhiteSpace(Controller) &&
            !string.IsNullOrWhiteSpace(Action);
    }
}
