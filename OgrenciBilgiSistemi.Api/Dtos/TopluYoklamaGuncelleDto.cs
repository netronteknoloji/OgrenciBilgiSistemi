namespace OgrenciBilgiSistemi.Api.Dtos
{
    // Toplu yoklama kaydetme isteği için kullanılan model
    public class TopluYoklamaGuncelleDto
    {
        public int SinifId { get; set; }
        public int PersonelId { get; set; }
        public int DersNumarasi { get; set; }
        public List<YoklamaKayitOgesiDto> Kayitlar { get; set; } = new();
    }
}
