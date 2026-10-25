# CLAUDE.md - OgrenciBilgiSistemi.Mobil

.NET MAUI 10 mobil uygulaması (Android + iOS). API'ye HTTPS + JWT ile bağlanır. Sistem geneli için root `CLAUDE.md`.

## Mimari

- Single project: `net10.0-android` (+ `net10.0-ios` yalnızca macOS'ta; Windows'ta iOS derlenmez).
- UI: `CommunityToolkit.Maui` + `CommunityToolkit.Mvvm`, Shell-based navigation.
- State: `Preferences.Default` (normal), `SecureStorage.Default` (hassas). Oturum: `Services/KullaniciOturum` (JWT + refresh token).
- HTTP: API servisleri `TemelApiService`'ten türer; tek paylaşımlı `HttpClient` (+ refresh için ayrı kısa timeout'lu client), 401'de refresh, başarısızsa `OturumSuresiDoldu` olayı.
- Konfig: `appsettings.json` **EmbeddedResource** olarak gömülü; `MauiProgram.YukleApiAyarlari()` `KayitSunucuUrl`'i okuyup `Preferences`'a yazar. Hata olursa `Constants.KayitSunucuUrl` fallback.
- Akış: Mobil → `KayitSunucuUrl` → okul seçimi (`OkulSecimView`, `OkulKayitServisi`) → `AktifOkulApiUrl` → login → JWT al.

## Push (FCM)

- `Plugin.Firebase.CloudMessaging` + `Plugin.LocalNotification` (ön planda gösterim).
- `PushKayitServisi`: token'ı `cihazlar/kaydet` / `token-yenile` ile API'ye kaydeder. `BildirimYonlendirmeServisi`: bildirime dokununca ilgili sayfaya yönlendirir.
- iOS'ta Firebase init plist'e bakmaz, koddan explicit `Firebase.Core.Options` ile yapılır (plist bundle köküne düşmüyordu).
- **Çıkış (logout) push token'ını SİLMEZ** — bilinçli karar; çıkıştan sonra da bildirim gelir, token sonraki girişte upsert ile yeniden eşlenir.
- Tanı: `PushTaniGunlugu` + `PushTaniView` (profil → "Bildirim Tanısı") bilinçli olarak sahada.

## Platform

| Platform | Min OS | RID | ApplicationId |
|---|---|---|---|
| Android | API 24 (Play otomatik koruma şartı) | `android-arm` + `android-arm64` (32-bit Android cihazlar için) | `com.netronyazilimvebilisimteknolojileri.ogrencibilgisistemi` |
| iOS | 15.0 | (auto) | `com.netronyazilim.ogrencibilgisistemi` |

⚠️ Bundle ID'ler **farklı** — store yayınında dikkat.

## Versiyon

- Güncel değer: csproj `ApplicationDisplayVersion` / `ApplicationVersion` (burada sabit yazılmaz). **Sürümü kullanıcı yönetir** — habersiz artırma.
- Artırılacaksa: yeni build no, csproj'daki değerin değil **git geçmişindeki en yüksek `ApplicationVersion`**'ın üstünde olmalı (sürüm geçmişte geriye sarıldı; App Store Connect aynı çifti ikinci kez kabul etmez):
  `for c in $(git log --format=%h -- OgrenciBilgiSistemi.Mobil/OgrenciBilgiSistemi.Mobil.csproj); do git show $c:OgrenciBilgiSistemi.Mobil/OgrenciBilgiSistemi.Mobil.csproj | grep -oP '(?<=<ApplicationVersion>)[^<]+'; done | sort -n | tail -1`
- `GuncellemeKontrolService` sürümü `AppInfo.Current.VersionString`'den okur; serviste güncelleme gerekmez. iOS `Info.plist`'te `CFBundleVersion` yok (csproj'dan üretilir).

## Tema / Görsel Kurallar

- Birden çok rolün açtığı ekran giren rolün rengini alır: VM'de `TemaRenk`/`TemaGradyan`, kaynak `Services/RolTema.cs`. Tek rollü ekran sabit rol kaynağını kullanır (`OgretmenRenk`, `VeliRenk`...).
- Semantik renklere dokunma (randevu durumu, Onayla/Reddet/İptal, yoklama çipleri, badge'ler). Yoklama matris renkleri `Shared/Constants/YoklamaRenkleri` — çip renkleriyle birebir eşit olmaması bilinçli.
- **DataTemplate içinde `BoxView`/`Frame` YASAK** → iOS CollectionView recycle'da renk kaybı; `Border` kullan.

## DI

- **Servisler:** Singleton (uygulama yaşam döngüsü tek instance)
- **View'lar:** Transient (Shell DI çözer)

**Başlıca servisler** (tam liste `Services/`): `GirisService`, `KullaniciOturum`, `OkulKayitServisi`, `SinifService`, `OgrenciService`, `ServisService`, `VeliService`, `RandevuService`, `OgretmenRandevuService`, `BildirimService`, `DuyuruService`, `OgretmenListeService`, `AdminService`, `VeliListeService`, `GenelAdminGirisGecisService`, `GuncellemeKontrolService`, `PushKayitServisi`, `BildirimYonlendirmeServisi`.

**View'lar** (tam liste `Views/`): giriş/okul seçimi, rol ana sayfaları (`SinifListeView`, `ServisEkraniView`, `VeliAnaSayfaView`, `AdminAnaSayfaView`), randevu, bildirim/duyuru, `Admin*` liste/detay ekranları, `PushTaniView`.

## Hassas Veri

| Veri | Yer |
|---|---|
| JWT, refresh token, şifre | `SecureStorage.Default` |
| `KayitSunucuUrl`, `AktifOkulApiUrl`, tercihler, kullanıcı adı | `Preferences.Default` |

## Yasaklar

- ❌ Servisi Transient/Scoped yapma → Singleton.
- ❌ Yeni `HttpClient` oluşturma → HTTP yalnızca `TemelApiService` üzerinden (türet ya da statik yardımcısını kullan).
- ❌ Token/şifreyi `Preferences`'a koyma → `SecureStorage`.
- ❌ Token'ı hardcode veya log'a yazma.
- ❌ `Constants.KayitSunucuUrl`'i sabit değer olarak değiştirme.
- ❌ View içinde iş mantığı → ViewModel/Service.
- ❌ Platform API'sini `#if ANDROID/#if IOS` olmadan çağırma.
- ❌ Yeni sayfa eklerken `MauiProgram.cs`'e `AddTransient` kaydını unutma.
- ❌ Versiyon artırmadan store'a yükleme (kural yukarıda).

## Komutlar

```bash
dotnet build OgrenciBilgiSistemi.Mobil -t:Run -f net10.0-android       # Çalıştır
dotnet build OgrenciBilgiSistemi.Mobil -t:Run -f net10.0-ios           # Çalıştır (macOS)
dotnet publish OgrenciBilgiSistemi.Mobil -f net10.0-android -c Release # AAB/APK
dotnet publish OgrenciBilgiSistemi.Mobil -f net10.0-ios -c Release     # IPA (macOS)
```

Store build'leri: `codemagic.yaml`.

## Klasörler

```
Services/   Views/   ViewModels/   Models/
Resources/{AppIcon, Fonts, Styles, Images, Raw, Splash}/
Platforms/{Android, iOS}/
App.xaml(.cs)   AppShell.xaml(.cs)   MauiProgram.cs   Constants.cs
appsettings.json (EmbeddedResource)
```

## Naming

- Servis: `*Service` (bazıları `*Servisi`) · View: `*View` · Kod dili: Türkçe.

## Paketler

`Microsoft.Maui.Controls` 10.0.x, `CommunityToolkit.Maui` 14.x, `CommunityToolkit.Mvvm` 8.x, `Plugin.Firebase.{Core, CloudMessaging}`, `Plugin.LocalNotification`. ProjectRef: `Shared` (Sms/Push YOK — push sunucu tarafı API/MVC'de).
