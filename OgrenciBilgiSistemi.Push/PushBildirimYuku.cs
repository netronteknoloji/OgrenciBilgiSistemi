namespace OgrenciBilgiSistemi.Push;

public sealed record PushBildirimYuku(
    string Baslik,
    string Govde,
    IReadOnlyDictionary<string, string> Veri);
