# Konum Takibi — Uygulama Planı

> Veli, mobil uygulamadan çocuğunun (öğrencinin) konumunu takip edebilsin.
> Analiz tarihi: 2026-07-07 · Durum: PLANLANDI (uygulanmadı)

## Mevcut Durum (Analiz Sonucu)

Sistemde konum takibi **hiç yok**:

- `KullaniciRolu` enum'unda öğrenci rolü yok → öğrenci uygulamaya login olamıyor (öğrenci hesap değil, veri kaydı).
- Dört projede de (Mobil, Api, MVC, Shared) enlem/boylam alanı, `Geolocation` çağrısı, harita bileşeni, konum izni yok.
- AndroidManifest'te konum izni yok; iOS `UIBackgroundModes` sadece `remote-notification`.
- Mobilde SignalR/polling yok; ekranlar veriyi `OnAppearing`'de HTTP ile çeker.
- Velinin bugün gördüğü en yakın şey: ZKTeco kart giriş-çıkış kayıtları (`OgrenciDetaylar`) + ana kapı geçişinde SMS.

## Hedef ve Kararlar

Öğrenci kendi telefonuna aynı MAUI uygulamayı kurup **Ogrenci** rolüyle login olur; uygulama **periyodik arka plan takip** modunda (~4 dk aralık, Android foreground service) konum gönderir. Veli, **WebView + Leaflet/OpenStreetMap** haritasında son konumu ve günlük rotayı görür.

| Karar | Seçim | Gerekçe |
|---|---|---|
| Öğrenci-kullanıcı bağı | Yeni `OgrenciProfilModel` (PK=`KullaniciId`, unique FK `OgrenciId`) | Veli/Servis/Ogretmen profil desenine birebir uyar (`AppDbContext.cs:237-268`); API login SQL'i zaten profil tablolarına LEFT JOIN atıyor (`Api\Services\GirisService.cs:38-41`); `Ogrenciler` tablosuna dokunulmaz |
| KVKK rızası | `OgrenciProfilModel.KonumTakibiOnay` (bool) + `KonumOnayTarihi` (DateTime?); API her konum POST'unda DB'den kontrol eder | Rıza geri çekilince aktif token'la bile yazma durur (403) |
| Konum tablosu | `OgrenciKonumModel` / DbSet `OgrenciKonumlar`, long PK, `(OgrenciId, KayitZamani)` index | 4 dk aralık → yüksek hacim; 30 gün saklama ile kontrol altında |
| Temizlik | MVC hosted service `OgrenciKonumTemizlemeService`: günlük tur, 30 günden eski kayıtları `ExecuteDeleteAsync` | `BildirimCihaziTemizlemeService` deseninin kopyası |
| Android izinleri | `ACCESS_FINE/COARSE_LOCATION` + `FOREGROUND_SERVICE` + `FOREGROUND_SERVICE_LOCATION`; **BACKGROUND_LOCATION istenmez** | Takip, kullanıcı uygulamayı açıkken başlatılan location tipli FGS ile; Play'in en ağır incelemesi tetiklenmez |
| Boot sonrası | BroadcastReceiver: API<34 servisi başlatır; API 34+ "uygulamayı açın" bildirimi gösterir | Android 14+ location FGS boot'tan başlatılamaz |
| iOS | `UIBackgroundModes: location` + CLLocationManager (DistanceFilter≈100 m, 3 dk throttle); **Android-önce fazlama** | iOS'ta timer tabanlı periyodik takip güvenilmez; App Store onayı ayrı risk |
| Harita | Tek self-contained `harita.html` (Leaflet JS+CSS inline gömülü, `Resources/Raw` MauiAsset), OSM tile'ları internetten, marker=divIcon | API key/Google Cloud hesabı gerekmez; `HtmlWebViewSource.Html` ile yüklenince android_asset/iOS bundle yol farkları kalkar |
| Canlı akış | 45 sn `IDispatcherTimer` polling + Yenile butonu | Mobilde SignalR yok; mevcut ekranlar da HTTP deseni |
| Idempotency | POST batch + SQL `WHERE NOT EXISTS (OgrenciId, KayitZamani)` dedup | Offline kuyruk yeniden gönderiminde çift kayıt olmaz |
| Rate limit | Yeni "konum" policy: **kullaniciId claim** partition'lı, ~30 istek/10 dk | Mevcut "arama" policy IP bazlı — okul NAT'ı arkasındaki öğrencileri yanlış keserdi |
| Öğrenci ekranında aç/kapa | YOK — sadece durum + KVKK bilgilendirme (prominent disclosure) | Rıza veliye aittir; öğrenci OS izniyle fiilen durdurabilir |
| Sürüm / menü seed | Dokunulmaz | Sürümü kullanıcı yönetir; öğrenci hesabı mevcut Kullanıcı İşlemleri ekranından açılır |

## Adımlar

### 1. Shared
- `Shared\Enums\KullaniciRolu.cs` → `[Display(Name = "Öğrenci")] Ogrenci = 6`.
- Yeni DTO'lar: `KonumNoktaDto` (Enlem, Boylam, Dogruluk?, BataryaSeviyesi?, KayitZamani), `KonumBildirimIstegiDto` (List — batch), `OgrenciKonumDto` (yanıt + SunucuZamani). Bu adımdan sonra üç proje birden derlenir.

### 2. MVC veri modeli + migration
- Yeni `Models\OgrenciProfilModel.cs`: `KullaniciId` (PK, `DatabaseGenerated(None)`), `OgrenciId` (FK), `KonumTakibiOnay`, `KonumOnayTarihi?`, `IsDeleted` — VeliProfil deseni.
- Yeni `Models\OgrenciKonumModel.cs`: long Identity PK, `OgrenciId` FK, `Enlem`, `Boylam`, `Dogruluk?`, `BataryaSeviyesi?`, `KayitZamani` (cihaz), `SunucuZamani`.
- `Data\AppDbContext.cs`: iki DbSet (çoğul Türkçe); OgrenciProfil 1:1 config + unique index `UX_OgrenciProfiller_OgrenciId` (bir öğrenciye tek hesap); OgrenciKonum `(OgrenciId, KayitZamani)` index + `IncludeDeleted` query filter (SinifYoklama deseni) + enlem/boylam check constraint'leri.
- `Models\KullaniciModel.cs`: `OgrenciProfil` navigation.
- Migration (sadece MVC): `dotnet ef migrations add OgrenciKonumTakibi` → idempotent script **her okul DB'sinde** çalıştırılır.

### 3. MVC admin akışı (öğrenci hesabı açma)
- `KullaniciService.EkleAsync/GuncelleAsync`'e Ogrenci rolü + OgrenciProfil upsert (VeliProfil bloğu deseni, satır 65-69 / 122-136); KVKK onayı işaretlenince `KonumOnayTarihi = DateTime.Now`; öğrenci seçimi için `GetOgrencilerSelectListAsync()`.
- `KullanicilarController` Guncelle GET switch'ine Ogrenci case + `GuncelleOgrenci` POST (`GuncelleVeli` deseni).
- `Views\Kullanicilar\Ekle/Guncelle.cshtml`: rol listesine Öğrenci; öğrenci seçim dropdown + "Veliden konum takibi için açık rıza alındı" checkbox.
- Şifreyi admin verir; öğrenci mobilden mevcut `sifre-degistir` endpoint'iyle değiştirir.

### 4. MVC temizlik job'ı
- Yeni `Services\BackgroundServices\OgrenciKonumTemizlemeService.cs`: `BildirimCihaziTemizlemeService` birebir desen (IServiceScopeFactory + günlük tur); 30 günden eski kayıtlar `IgnoreQueryFilters().ExecuteDeleteAsync`. `Program.cs`'e `AddHostedService`. Logda yalnız silinen adet — **koordinat asla loglanmaz**.

### 5. API (ham SQL — EF yasak)
- `Services\GirisService.cs` (her iki KimlikDogrula metodu — refresh akışı da claim üretir): `LEFT JOIN OgrenciProfiller OGP`; SELECT'e `OGP.OgrenciId`, `OGP.KonumTakibiOnay`. `Api\Models\KullaniciModel.cs`'e alanlar.
- `KimlikDogrulamaController.GenerateJwtToken` (satır 279-287): rol switch'ine `Ogrenci` — **eklenmezse default kolu exception fırlatır, öğrenci login patlar**. `ogrenciId` claim'i (satır 299-303 servisId/veliId deseni; değer = profildeki gerçek OgrenciId, KullaniciId değil). Profilsiz Ogrenci → Unauthorized. Login yanıtına `OgrenciId`.
- Yeni `IKonumService`/`KonumService` (Scoped, `TenantBaglami.ConnectionString`, `Microsoft.Data.SqlClient`):
  - `KonumlariKaydetAsync`: nokta başına validasyon (enlem −90..90, boylam −180..180, gelecek tarih reddi, 7 günden eski nokta atılır) + `INSERT ... WHERE NOT EXISTS` dedup; `SunucuZamani = GETDATE()`.
  - `KonumOnayVarMiAsync`, `SonKonumGetirAsync` (TOP 1), `GunlukKonumlariGetirAsync` (gün aralığı, TOP 1000). Dönüşler DTO (domain modeli direkt dönme yasağı).
- Yeni `KonumController` (`[Authorize]` manuel — global fallback yok):
  - `POST api/konumlar` `[EnableRateLimiting("konum")]`: rol≠Ogrenci → Forbid; onay yok → Forbid; yanıt `{ kaydedilen: n }`.
  - `GET api/ogrenciler/{id}/konumlar/son` ve `GET .../konumlar?tarih=yyyy-MM-dd`: Veli için `ogrenci.VeliId != veliId → Forbid()` (`GecisKayitController.cs:81-84` birebir); Admin/GenelAdmin serbest; kayıt yoksa 204.
- `Api\Program.cs`: `AddScoped<IKonumService, KonumService>` + kullaniciId claim partition'lı "konum" rate limit policy.
- Hata loglarında koordinat/istek gövdesi yazılmaz — yalnız OgrenciId + hata tipi.

### 6. Mobil ortak
- `Services\KullaniciOturum.cs`: `OgrenciId` alanı + `OgrenciMi` yardımcısı (satır 261-264 bloğu), oturum yükle/temizle güncellemesi.
- `Services\GirisService.cs`: login yanıtından `OgrenciId` oku → oturuma yaz.
- `ViewModels\GirisGorunumModel.cs:165-172`: `OgrenciMi → ///OgrenciAnaSayfaView` dalı (VeliMi'den sonra, else'ten önce).
- `AppShell.xaml` rota; `RolTema`'ya "Ogrenci" dalı + `Colors.xaml`'e `OgrenciRenk`/`OgrenciSoft`/`OgrenciBaslikGradyan`; `MauiProgram.cs` DI kayıtları (servisler Singleton, View'lar Transient).

### 7. Mobil öğrenci tarafı (arka plan takip)
- Yeni `Services\KonumGondericiServisi.cs` (`TemelApiService` türevi — JWT/refresh/401 akışı bedava): offline kuyruk `FileSystem.AppDataDirectory\konum_kuyruk.json` (cap 200), bağlantı varken batch POST (dedup sunucuda), son gönderim zamanı Preferences'ta.
- Yeni `IKonumTakipServisi` (`Baslat/Durdur/CalisiyorMu`) + `#if ANDROID / #if IOS` implementasyonlar.
- Yeni `OgrenciAnaSayfaView` + VM: durum kartı, son gönderim, KVKK metni; OnAppearing'de **prominent disclosure** diyaloğu → izin → servis başlat + `Preferences "KonumTakipAktif"`; şablonda Border (BoxView/Frame yasak), RolTema; logout'ta servis durur.
- **Android**: Manifest'e 4 izin; `Platforms\Android\Services\KonumTakipServisi.cs` — `[Service(Exported=false, ForegroundServiceType=ForegroundService.TypeLocation)]`, kalıcı bildirim ile `StartForeground` (API-sürüm kontrollü overload, minSdk 23), `START_STICKY`; 4 dk döngüde kısa `PartialWakeLock` içinde `Geolocation.GetLocationAsync(Medium, 30 sn)` + `Battery.ChargeLevel`. `KonumBootAlicisi` BroadcastReceiver (BOOT_COMPLETED zaten var): API<34 → `StartForegroundService`; API 34+ → "uygulamayı açın" bildirimi; SecureStorage değil Preferences bayrağına bakar. OgrenciAnaSayfa OnAppearing'de servis çalışmıyorsa yeniden başlat (kill/güncelleme toparlaması).
- **iOS** (ayrı dağıtım fazı — Android önce): Info.plist `UIBackgroundModes location` + `NSLocationWhenInUse/AlwaysAndWhenInUseUsageDescription` (Türkçe); CLLocationManager `AllowsBackgroundLocationUpdates`, DistanceFilter 100 m, 3 dk gönderim throttle, significant-change yedek; izin akışı WhenInUse → Always.

### 8. Mobil veli tarafı (harita)
- Yeni `Resources\Raw\harita.html`: Leaflet 1.9.x JS+CSS inline, OSM tile layer (attribution zorunlu). JS API: `sonKonumGoster(lat, lng, zamanMetni, dogruluk)` (divIcon marker + doğruluk çemberi), `rotaCiz(noktalarJson)` (polyline + fitBounds), `temizle()`.
- Yeni `Services\KonumService.cs` (Singleton, TemelApiService türevi): son konum (204→null) + günlük rota.
- Yeni `OgrenciKonumView` + `OgrenciKonumGorunumModel` (runtime parametreli — DI'a kaydedilmez, OgrenciDetayGorunumModel deseni): bilgi şeridi (son görülme/doğruluk/batarya, Border) + DatePicker + Yenile + WebView; `FileSystem.OpenAppPackageFileAsync("harita.html")` → `HtmlWebViewSource.Html`; koordinatlar **`CultureInfo.InvariantCulture`** ile `EvaluateJavaScriptAsync`'e basılır (Türkçe locale virgülü JS'i kırar); 45 sn `IDispatcherTimer` polling (OnAppearing başlat / OnDisappearing durdur); boş durum metni (API 204/403).
- `OgrenciDetayView`: yalnız `KullaniciOturum.VeliMi` iken görünen "Konumu Gör" butonu → `OgrenciKonumView(ogrenciId, ad)`.

### 9. Store / KVKK (kod dışı kontrol listesi)
- **Google Play**: Data safety → Location "collected, not shared"; `FOREGROUND_SERVICE_LOCATION` için Play Console FGS declaration + kullanım videosu (disclosure → izin → kalıcı bildirim akışı). BACKGROUND_LOCATION istenmediğinden ağır inceleme tetiklenmez.
- **App Store**: purpose string'ler + Review Notes'ta veli-öğrenci senaryosu ve test hesabı; `location` background mode gerekçesi.
- **KVKK**: aydınlatma metinleri (öğrenci + veli ekranı), açık rıza kaydı (`KonumTakibiOnay` + `KonumOnayTarihi`), saklama 30 gün (temizlik job'ı), koordinat hiçbir logda/SMS'te yer almaz.
- **Sürüm**: `ApplicationDisplayVersion/ApplicationVersion` bu planda değiştirilmez (kullanıcı yönetir).

## Sıra ve Bağımlılıklar

1 (Shared) → 2 (MVC model+migration) → {3, 4 (MVC), 5 (API)} paralel → 6 (Mobil ortak) → {7 (öğrenci), 8 (veli)} paralel → 9 (uyumluluk).

⚠️ API konum endpoint'leri, migration **her okul DB'sine** uygulanmadan canlıya alınmaz (API ham SQL — tablo yoksa runtime hatası).

## Test / Doğrulama

1. **Derleme**: Shared + MVC (x86) + API + Mobil (`dotnet build OgrenciBilgiSistemi.Mobil -f net10.0-android`). Enum değişikliği üç projeyi de kırmamalı.
2. **Migration**: dev DB'de update; idempotent script ikinci çalıştırmada hatasız; unique index aynı öğrenciye ikinci hesabı engeller.
3. **MVC**: Öğrenci rollü kullanıcı ekle/güncelle → `OgrenciProfiller` satırı + onay tarihi; öğrenci silme (Restrict) davranışı.
4. **API (Swagger)**: öğrenci login → `rol=Ogrenci` + `ogrenciId` claim; profilsiz → 401; POST dedup (`kaydedilen: 0`); onay kapalı → 403; veli token'ıyla POST → 403; yabancı veli GET → 403; kayıt yok → 204; geçersiz koordinat → 400; limit → 429.
5. **Android emülatör**: disclosure → izin → kalıcı bildirim; mock location → ~4 dk'da DB kaydı; uçak modu → kuyruk birikir, ağ dönünce batch; kill → START_STICKY devam; reboot davranışı (API<34 / 34+); logout → servis durur.
6. **Veli**: marker + son görülme; tarih seç → polyline; 45 sn otomatik yenileme; verisiz/onaysız öğrencide boş durum.
7. **Temizlik**: 31 gün eski satır gece turunda silinir; logda yalnız adet.

## Riskler

- **OEM pil optimizasyonu** (Xiaomi/Samsung vb.): FGS'ye rağmen aralık gecikebilir → öğrenci ekranından pil optimizasyonu ayar sayfasına yönlendirme intent'i eklenebilir (REQUEST_IGNORE_BATTERY_OPTIMIZATIONS izni İSTENMEZ — Play'de hassas).
- **Android 14+ boot kısıtı**: reboot sonrası takip, öğrenci uygulamayı açana kadar durur (bildirimle hafifletilir).
- **iOS güvenilirliği + App Store reddi**: arka plan konum gerekçesi reddedilebilir → Android-önce fazlama.
- **Tablo büyümesi**: 500 öğrencili okulda 30 günde birkaç milyon satır üst sınır — index + gece `ExecuteDeleteAsync` yeterli.
- **Kültür hatası**: JS'e koordinat basarken InvariantCulture şart.
- **Mobil CLAUDE.md sürüm uyuşmazlığı**: CLAUDE.md "MAUI 9 / v2.5" diyor, csproj net10.0 / 3.9.15-79 — csproj esas alınır.
- **SecureStorage boot erişimi**: BOOT_COMPLETED kilit açılmadan gelebilir → receiver Preferences bayrağına bakar, token'a servis içinde ihtiyaç anında erişilir.

## Kritik Dosyalar

- `OgrenciBilgiSistemi\Data\AppDbContext.cs`
- `OgrenciBilgiSistemi\Services\Implementations\KullaniciService.cs`
- `OgrenciBilgiSistemi.Api\Controllers\KimlikDogrulamaController.cs` · `Api\Services\GirisService.cs`
- `OgrenciBilgiSistemi.Mobil\Services\KullaniciOturum.cs` · `ViewModels\GirisGorunumModel.cs`
- `OgrenciBilgiSistemi.Mobil\Platforms\Android\AndroidManifest.xml` · `Platforms\iOS\Info.plist`
