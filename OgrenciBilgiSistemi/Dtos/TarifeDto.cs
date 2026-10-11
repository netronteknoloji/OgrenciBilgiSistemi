namespace OgrenciBilgiSistemi.Dtos
{
    public sealed class TarifeDto
    {
        public int Yil { get; set; }
        public decimal Tutar { get; set; }
        public string? Aciklama { get; set; }

        // true => ödemesi olan ve özel tutarı olmayan öğrenciler eski tarifede sabitlenir
        public bool OdeyenleriSabitle { get; set; }
    }
}
