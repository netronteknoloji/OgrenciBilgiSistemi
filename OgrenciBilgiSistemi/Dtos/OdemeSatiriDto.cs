namespace OgrenciBilgiSistemi.Dtos
{
    public sealed class OdemeSatiriDto
    {
        public int OgrenciAidatOdemeId { get; set; }
        public DateTime Tarih { get; set; }
        public decimal Tutar { get; set; }
        public string? Aciklama { get; set; }
    }
}
