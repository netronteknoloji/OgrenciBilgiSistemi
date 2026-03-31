namespace OgrenciBilgiSistemi.Dtos
{
    public enum RaporDurumFiltresiDto
    {
        Hepsi = 0,
        Borclu = 1,   // Kalan > 0 ve Muaf = false
        Borcsuz = 2,  // Kalan = 0 (Muaf true/false fark etmez)
        Muaf = 3      // Muaf = true
    }
}
