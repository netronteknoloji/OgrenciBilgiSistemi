namespace OgrenciBilgiSistemi.ViewModels
{
    public class MenuOgeAtamaVm
    {
        public int MenuOgeId { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public bool AtandiMi { get; set; }

        public int? AnaMenuId { get; set; }
        public List<MenuOgeAtamaVm> AltOgeler { get; set; } = new();
    }
}
