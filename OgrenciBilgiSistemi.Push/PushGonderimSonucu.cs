namespace OgrenciBilgiSistemi.Push;

public sealed record PushGonderimSonucu(
    int BasariliSayisi,
    int BasarisizSayisi,
    IReadOnlyList<string> GecersizTokenlar);
