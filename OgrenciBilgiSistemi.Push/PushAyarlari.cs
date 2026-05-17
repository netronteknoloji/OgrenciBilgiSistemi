namespace OgrenciBilgiSistemi.Push;

public sealed class PushAyarlari
{
    public const string SectionName = "Push";

    public string ServiceAccountJsonYolu { get; set; } = "";
    public bool Aktif { get; set; } = true;
    public int ZamanAsimiSaniye { get; set; } = 30;
    public string AndroidVarsayilanKanal { get; set; } = "obs_default";
}
