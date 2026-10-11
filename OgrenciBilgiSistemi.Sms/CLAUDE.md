# CLAUDE.md - OgrenciBilgiSistemi.Sms

ilksms.com sağlayıcısına HTTP üzerinden SMS atan, MVC ve API'nin paylaştığı bağımsız class library. Persistence yok.

## Mimari

- `net10.0`, sadece `Microsoft.Extensions.{Http, Options.ConfigurationExtensions, Logging.Abstractions}` 10.0.
- Typed `HttpClient`: `AddHttpClient<ISmsService, SmsService>` (`IHttpClientFactory`).
- ProjectReference YOK (Shared'e bile bağımlı değil).

## Kullanım

```csharp
// Program.cs (MVC ve API'de aynı)
builder.Services.AddSmsAltyapisi(builder.Configuration);
```

**SMS outbox üzerinden gider** (root `CLAUDE.md`). Üretici (`IGonderimKuyruguService` — MVC, `YoklamaSmsBildirimService` — API) yalnızca `GonderimKuyrugu`'na satır ekler; `ISmsService.Gonder`'i **yalnızca** MVC'deki `GonderimKuyruguIsleyici` çağırır. Yeni bir yerden doğrudan `ISmsService` enjekte edip gönderme.

- Mesaj metinleri: `SmsMesajSablonlari` (üreticiler kuyruğa eklerken kullanır).
- Sonuç: `SmsGonderimSonucu` + `SmsHataKategorisi` (Gecici/Kalici) → işleyici tekrar deneyip denemeyeceğine buna göre karar verir (`Shared/Kurallar/GonderimKuyruguKurallari`).
- Toplu gönderim: `SmsParalelGonderici.GonderHerBiri` (`SemaphoreSlim`, `MaxParalelGonderim`); dönen listeyi çağıran tek thread'de işler (DbContext thread-safe değil).

## Konfigürasyon

`appsettings.json` → `SmsAyarlari` (MVC + API'de aynı):

```json
{
  "SmsAyarlari": {
    "ApiUrl": "https://app.ilksms.com/api/ntonsms",
    "KullaniciAdi": "...",
    "Sifre": "...",
    "Baslik": "...",              // 11 karakter, alfanumerik
    "ZamanAsimiSaniye": 30,
    "TurkceKarakter": true,
    "Aktif": true,                // false → gönderim yok (başarılı sayılır, test)
    "MaxParalelGonderim": 5
  }
}
```

## Sorumluluk Sınırı

Bu kütüphane **sadece gönderir**, sonucu döndürür. Yapmadıkları:
- **Persistence yok** — kuyruk `GonderimKuyruguModel`, geçmiş `SmsGonderimGecmisiModel` (MVC, EF).
- **Retry yok** — tekrar deneme/geri çekilme MVC `GonderimKuyruguIsleyici` + `GonderimKuyruguKurallari`'nın işi.

## Yasaklar

- ❌ EF Core / DB import.
- ❌ `Microsoft.AspNetCore.*` bağımlılığı.
- ❌ `new HttpClient()` → typed client.
- ❌ `KullaniciAdi`/`Sifre` hardcode veya log'a yazma.
- ❌ Telefon numarası, mesaj içeriği loglama.
- ❌ `MaxParalelGonderim`'i config'siz sabitleme.
- ❌ `Task.WhenAll` ile `SemaphoreSlim`'siz paralel gönderim → ban riski (`SmsParalelGonderici` kullan).
- ❌ `Aktif: false` kontrol etmeden gönderim.
- ❌ `TurkceKarakter: false` iken dönüşüm yapmadan TR karakterli mesaj yollama (karakter/maliyet hesabı bozulur).

## Loglanabilir / Loglanamaz

- ✅ Sağlayıcı yanıt kodu, gönderim süresi, başarı/başarısızlık, hata kategorisi
- ❌ Telefon numarası (PII), mesaj içeriği (hassas)

## Yeni Sağlayıcı Ekleme

`ISmsService` kontratını koru, yeni `XyzSmsService` yaz, `AddSmsAltyapisi` içinde seç. (Şu an tek sağlayıcı var; seçim için config anahtarı henüz yok — gerekirse `SmsAyarlari`'na eklenir.)

## Klasörler

```
ISmsService.cs (+ SmsGonderimSonucu)   SmsService.cs   SmsAyarlari.cs   SmsAltyapiExtensions.cs
SmsHataKategorisi.cs   SmsMesajSablonlari.cs   SmsParalelGonderici.cs
```

## Naming

- Tipler Türkçe (`SmsAyarlari`, `SmsMesajSablonlari`, `AddSmsAltyapisi`)
- Kod dili: Türkçe.
