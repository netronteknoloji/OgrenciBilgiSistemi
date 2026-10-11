# OgrenciBilgiSistemi — Uçtan Uca Denetim Raporu

## Yönetici Özeti

**Sistem:** Okul başına ayrı SQL Server veritabanı kullanan çok kiracılı (multi-tenant) bir öğrenci takip sistemi. Aynı IIS sunucusunda x86 çalışan iki uygulamadan oluşuyor: MVC yönetim paneli (EF Core, ZKTeco kart okuyucu, SignalR kiosk ekranı, 6 arka plan servisi) ve mobil REST API (JWT, ham SQL). Mobil uygulama MAUI ile yazılmış (Android + iOS). Ayrı bir Worker servisi yok, ASP.NET Identity kullanılmıyor, otomatik test yok, konum özelliği yok. Repodaki yapılandırmada bugün **1 okul** tanımlı.

**Bulgu sayıları (birleştirilmiş):** **20 Yüksek**, ~54 Orta, ~19 Düşük (Düşük satırların bir kısmı gruplanmıştır).

**En kritik riskler:**
1. **Panelde yetki yükseltme (AUTH-01, AUTH-02, AUTH-03).** Okul Admin'i kendine "Genel Yönetici" rolü verip tüm okulların verisine erişebiliyor. Yalnızca bir rapor menüsü yetkisi olan kullanıcı, öğrenci formuna alan ekleyerek yeni bir Admin hesabı açabiliyor.
2. **Mobil API'de yetki ve KVKK açıkları (SEC-01, SEC-02, AUTH-05).**
   - Servis şoförü herhangi bir öğrenci için yoklama yazıp veliye sahte SMS gönderebiliyor.
   - Öğretmen, okuldaki tüm öğrencilerin veli adresini, telefonunu, iş yerini ve kart UID'sini alabiliyor.
   - Pasife alınmış veli, çocuğunun verisini görmeye devam ediyor.
   - Veli tarafındaki temel sahiplik kontrolleri ise doğru çalışıyor: veli başka öğrencinin verisine erişemiyor (bkz. 3.6).
3. **Aktarım güvenliği ve sızmış kimlik bilgileri (SEC-06, SEC-07, AUTH-04).**
   - Mobil uygulama okul listesini ve fallback API'yi düz HTTP ile kullanıyor.
   - Login'de deneme sınırı yok ve minimum şifre 4 karakter.
   - Git geçmişinde duran SMS şifresi ile GenelAdmin hash'i **hâlâ geçerli**; rotasyon yapılmamış.
4. **Kart okuma verisinin güvenilirliği (WRK-02, WRK-03, WRK-04, WRK-05).**
   - Cihaz bağlantısı koptuğunda bu fark edilmiyor.
   - Gerçek zamanlı eşleştirme tek bir cihaz olayına (`HIDNum`) bağımlı; diğer olay (`AttTransactionEx`) yanlış alanla arandığı için boşa düşüyor.
   - DB hatasında okutma kayboluyor.
   - Yemekhane servisi cihaz logunu siliyor.
   - Bunlar yoklama verisinin sessizce eksik veya yanlış olmasına yol açabilir.
5. **Okullar arası izolasyon (WRK-01, SEC-03, SR-01).**
   - Arka plan işleri ve fire-and-forget SMS'ler her zaman ilk okulun DB'sini kullanıyor.
   - SignalR yayınları tüm oturumlara gidiyor.
   - İkinci okul eklendiği anda doğrudan veri sızıntısına dönüşür.
   - SignalR yayınları bugün de okul içinde veli ve servis hesaplarına gidiyor.

**Olumlu yönler:**
- SQL injection bulunmadı: ham SQL parametreli, dinamik kısımlar sabit whitelist.
- `Html.Raw` yok. CSRF koruması MVC'de kapsamlı.
- Şifreler PBKDF2 (Identity v3 hasher) ile saklanıyor.
- JWT doğrulama parametreleri tam. Tenant yalnızca imzalı claim'den çözülüyor.
- Token'lar mobilde SecureStorage'da. Backend'de `.Result`/`.Wait`/`async void` yok.
- Cache anahtarları tenant'a göre ayrılmış. Biyometrik veri sisteme aktarılmıyor.
- Mobil ile API DTO'ları arasında kırıcı uyumsuzluk yok.

**Karar:**
- Mevcut tek okullu kurulum için **koşullu GO**: ilk 5 iş 1-2 hafta içinde tamamlanmalı.
- İkinci okulun eklenmesi için **NO-GO**.
- Ayrıntılar §6'da.

## 0. Kapsam ve Yöntem

- **Tarih:** 2026-09-27 · **HEAD:** `ef70ebe` · **Çalışma ağacı kirli:** commit'lenmemiş aidat/kullanıcı/öğretmen değişiklikleri ve 2 yeni migration (`20260927092659_AidatOdenenKaldirVeTutarKurali`, `20260927111253_KullaniciAdiBenzersiz`) denetime **dahildir**. Satır numaraları çalışma ağacına göredir.
- **Yöntem:** statik kod incelemesi (4 paralel alt ajan: API, MVC, ZKTeco/SignalR/arka plan, Mobil+config) + araç çıktıları. Yüksek öncelikli bulgular ana denetçi tarafından dosya açılarak yeniden doğrulanmıştır. Çalışma zamanı testi yapılmadı; çalışma zamanı davranışına dayanan iddialar "doğrulanmadı" olarak işaretlidir.
- **Kaynak kod değiştirilmedi.** Yalnızca bu dosya yazıldı (build çıktıları gitignore'daki `bin/obj` altına düştü).
- **Secret politikası:** Bu raporda hiçbir şifre/anahtar/connection string **değeri** yer almaz; yalnızca dosya:satır ve anahtar adı.

### 0.1 Görev tanımındaki varsayımlar vs. koddaki gerçek

| Varsayım (görev tanımı) | Koddaki gerçek | Kanıt |
|---|---|---|
| Ayrı Worker Service projesi ZKTeco verisini çeker | **Ayrı Worker yok.** ZKTeco okuma MVC web uygulaması içindeki hosted service'lerde çalışır (IIS app pool geri dönüşünde okuma da durur) | `OgrenciBilgiSistemi/Program.cs:123-128` |
| ASP.NET Core Identity + JWT | **Identity yok.** MVC: cookie auth + özel `KimlikDogrulamaService`; GenelAdmin `appsettings`'te. API: özel JWT + in-memory `RefreshTokenService` (Singleton) | `OgrenciBilgiSistemi/Program.cs:136-152`, `OgrenciBilgiSistemi.Api/Program.cs:62-88` |
| Konum verisi (servis/öğrenci) | **Konum özelliği yok**; `KONUM_TAKIBI_PLANI.md` yalnızca plan. Konum ile ilgili maddeler "uygulanmamış" olarak değerlendirildi | — |
| SignalR gerçek zamanlı veli bildirimleri | SignalR **yalnızca MVC panelinde** kart okuma ekranı için (`KartOkuHub`); mobil gerçek zamanlılık FCM push ile | `OgrenciBilgiSistemi/Program.cs:131,193` |
| Test projeleri | **Test projesi yok** (çözümde 6 proje, hiçbiri test) | `OgrenciBilgiSistemi.sln` |
| .NET 10 | Tüm projeler `net10.0` / `net10.0-android;net10.0-ios`. **Not:** `OgrenciBilgiSistemi/CLAUDE.md` ve `OgrenciBilgiSistemi.Api/CLAUDE.md` hâlâ ".NET 9", "JwtBearer 9.0.0", "Swashbuckle 6.6.2" diyor → dokümantasyon eskimiş | csproj'lar |

---

## 1. AŞAMA 1 — Keşif ve Araçlar

### 1.1 Mimari özet

```
                    ┌──────────── Tek IIS sunucusu (x86 app pool) ────────────┐
Yönetici tarayıcı ─▶│ MVC Panel (cookie auth, EF Core 10, SignalR KartOkuHub,  │──┐
                    │   6 hosted service: ZKTeco olay/izleyici, yemekhane,     │  │  Okul başına
ZKTeco cihazları ◀─▶│   SMS retry, randevu, bildirim cihazı temizleme)         │  ├─▶ ayrı SQL Server DB
                    │ REST API (JWT, ham SqlClient, 1 hosted svc: SMS retry,   │  │  (Okullar[] config)
MAUI mobil ────────▶│   MVC wwwroot/uploads'u statik sunar)                    │──┘
 (Veli/Öğretmen/    └──────────────────────────────────────────────────────────┘
  Şoför/Admin)            │ ilksms.com (SMS)   │ Firebase FCM (push)
```

- **Projeler ve referanslar:** MVC → Shared, Sms, Push · API → Shared, Sms, Push · Mobil → (HTTP ile API) · MVC ↔ API arasında proje referansı yok, **aynı DB'yi paylaşırlar** ve iş mantığı iki kez yazılmıştır (EF Core vs ham SQL).
- **Admin panel veri erişimi:** MVC, API'yi kullanmaz; `AppDbContext` üzerinden doğrudan tenant DB'sine bağlanır. Tenant, `MvcTenantCozumleyiciMiddleware` ile cookie'deki `okulKodu` claim'inden (GenelAdmin için session'daki `SeciliOkulKodu`'ndan) çözülür (`OgrenciBilgiSistemi/Infrastructure/MvcTenantCozumleyiciMiddleware.cs:22-44`).
- **DbContext:** `AddDbContext` per-request connection string; tenant çözülemezse **ilk okulun** connection string'ine düşer (`OgrenciBilgiSistemi/Program.cs:45-57`) — bkz. bulgu listesi.
- **API tenant:** JWT `okulKodu` claim'i → `TenantCozumleyiciMiddleware`; anonim isteklerde tenant boş kalır (`OgrenciBilgiSistemi.Api/Middleware/TenantCozumleyiciMiddleware.cs:17-21`).
- **Auth:** MVC cookie (8 saat, sliding) + global `AuthorizeFilter` + `MenuYetkiFilter` + `TenantZorunluFilter` (`OgrenciBilgiSistemi/Program.cs:63-68,136-152`). API JWT (issuer/audience/lifetime/key doğrulamalı), **global FallbackPolicy yok**, `AdminOnly` = `RequireClaim("rol", …)` (`OgrenciBilgiSistemi.Api/Program.cs:62-80`).
- **DI ömürleri:** Singleton: `IZKTecoService`, `OkulYapilandirmaServisi`, `RefreshTokenService`, `TimeProvider`. Scoped: `TenantBaglami`, `AppDbContext`, tüm domain servisleri.
- **Cache:** `AddMemoryCache` (MVC), `AddDistributedMemoryCache` (yalnızca session için), refresh token'lar süreç belleğinde.
- **Dağıtım:** MVC + API tek IIS, x86 app pool (ZKTeco `zkemkeeper` COM 32-bit). API, MVC `wwwroot`'unu `MvcWwwRootPath` veya hardcode `C:\inetpub\wwwroot\obs\wwwroot` fallback'i ile sunar (`OgrenciBilgiSistemi.Api/Program.cs:142-160`). Mobil Google Play / App Store.

### 1.2 Araç çıktıları

| Komut | Sonuç |
|---|---|
| `dotnet build` Shared / Sms / Push | ✅ 0 hata, 0 uyarı |
| `dotnet build` Api | ✅ 0 hata; 2× **NU1903** `Microsoft.OpenApi 2.4.1` (Yüksek önem, GHSA-v5pm-xwqc-g5wc) |
| `dotnet build` MVC | ❌ `dotnet build` COM referansı nedeniyle çalışmaz (MSB4803). VS MSBuild `-t:Rebuild -p:Platform=x86` ile: ✅ 0 hata, 38 uyarı (aşağıda) |
| `dotnet build` Mobil (`net10.0-android`) | (aşağıda 1.2.2) |
| `dotnet test OgrenciBilgiSistemi.sln` | Çalıştı, **0 test** (test projesi yok) → test kapsamı %0 |
| `dotnet list package --vulnerable --include-transitive` | Api: `Microsoft.OpenApi 2.4.1` **High** (Swashbuckle 10.1.7 üzerinden geçişli) · MVC: `SQLitePCLRaw.lib.e_sqlite3 2.1.11` **High** (GHSA-2m69-gcr7-jv3q; `Microsoft.EntityFrameworkCore.Sqlite` üzerinden) · Shared/Sms/Push/Mobil: yok |
| `dotnet list package --outdated` | Tümü patch/minor seviyesinde geride (aşağıda) — major sürüm uyumsuzluğu yok |

#### 1.2.1 MVC derleme uyarıları (VS MSBuild, x86 Rebuild)

| Kod | Adet | Örnek konum | Değerlendirme |
|---|---|---|---|
| CS8601/CS8602/CS8604/CS8618/CS8629 | 26 | `Controllers/KartOkuController.cs:139,167,195,231,274`; `Services/BackgroundServices/KartOkumaOlayIsleyiciService.cs:121`; `Services/Implementations/CihazService.cs:224,318`; `Services/Implementations/YemekhaneService.cs:521-522`; `Views/OgrenciGirisCikis/Detay.cshtml:95-97,124,156,190,211`; `Views/Shared/Components/Menu/Default.cshtml:57,65` | Null-reference riski; kart okuma ve yemekhane akışlarında olanlar üretimde `NullReferenceException` üretebilir (bkz. BUG bulguları) |
| CA1416 | 4 | `Marshal.FinalReleaseComObject` — `YemekhanePollingService.cs:256,343`, `CihazService.cs:42`, `ZKTecoService.cs:248` | Proje Windows'a bağlı; `<SupportedOSPlatform>windows` eksik (Düşük) |
| MVC1000 | 2 | `Views/Kullanicilar/_MenuTree.cshtml:24`, `Views/Kullanicilar/YetkiGuncelle.cshtml:31` | Senkron `Html.Partial` → thread pool deadlock/tıkanma riski; `<partial>` kullanılmalı (Düşük) |
| CS0618 | 1 | `OgrenciBilgiSistemi.Push/PushAltyapiExtensions.cs:49` | `GoogleCredential.FromFile` **obsolete** (güvenlik gerekçesiyle) → `CredentialFactory` (Düşük) |
| MSB3305 | 4 | `zkemkeeper` COM interop | COM tip kütüphanesi uyarısı, bilinen durum |
| NU1903 | 1 | `SQLitePCLRaw.lib.e_sqlite3` | Bkz. vulnerable |

#### 1.2.2 Mobil derleme

`dotnet build -f net10.0-android --no-incremental`: ✅ 0 hata, 154 uyarı. iOS hedefi Windows'ta derlenemedi (**doğrulanmadı**).

| Kod | Değerlendirme |
|---|---|
| CS0618 (obsolete, en çok) | `Application.MainPage` (get/set) ve `Page.DisplayAlert`/`DisplayActionSheet` — MAUI 10'da obsolete; `Windows[0].Page` ve `DisplayAlertAsync`/`DisplayActionSheetAsync` ile değiştirilmeli. Örnek: `OgrenciBilgiSistemi.Mobil/App.xaml.cs:65`, `ViewModels/GirisGorunumModel.cs:128,134,147,176,181`, `ViewModels/AdminAnaSayfaGorunumModel.cs:111`, `Services/GuncellemeKontrolService.cs:36,39` |
| CA1422 | `Platforms/Android/MainActivity.cs:43` — eski Android API çağrısı |
| CS86xx (nullable) | ~80 uyarı; ViewModel'lerde null atamaları |

ViewModel'lerin `Application.Current.MainPage.DisplayAlert` ile doğrudan UI'ye erişmesi ayrıca test edilebilirliği düşürür (bkz. MOB bulguları).

#### 1.2.3 Güncel olmayan paketler (tümü .NET 10 uyumlu)

| Proje | Paket | Mevcut → Son |
|---|---|---|
| Api | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.8 → 10.0.12 |
| Api | Microsoft.Data.SqlClient | 7.0.1 → 7.1.0 |
| Api | Swashbuckle.AspNetCore | 10.1.7 → 10.2.3 (geçişli OpenApi zafiyetini kapatıp kapatmadığı **doğrulanmadı**; gerekirse `Microsoft.OpenApi` doğrudan yükseltilmeli) |
| MVC | Microsoft.EntityFrameworkCore(.SqlServer/.Sqlite/.Tools) | 10.0.9 → 10.0.12 |
| MVC | ClosedXML 0.105.0 → 0.105.1 · Polly 8.6.6 → 8.8.0 · System.IO.Ports 10.0.8 → 10.0.12 | |
| Shared/Sms/Push | Microsoft.Extensions.* | 10.0.8 → 10.0.12 |
| Push | FirebaseAdmin | 3.5.0 → 3.7.0 |
| Mobil | Microsoft.Maui.Controls(.Compatibility) | 10.0.70 → 10.0.110 |
| Mobil | CommunityToolkit.Maui | 14.1.1 → **15.0.1 (major)** |
| Mobil | Plugin.LocalNotification 14.1.0 → 14.1.2 · Microsoft.Extensions.Logging.Debug 10.0.8 → 10.0.12 | |

**Obsolete API:** Tek derleyici tespiti `GoogleCredential.FromFile` (CS0618). MVC'de `Microsoft.EntityFrameworkCore.Sqlite` paketi SQL Server kullanan üretim kodu için gereksiz görünüyor ve zafiyetli native SQLite'ı çekiyor (kullanım yeri Aşama 2'de kontrol edildi, bkz. bulgular).

### 1.3 Aşama 1 bulguları

| ID | Öncelik | Kategori | Dosya:Satır | Sorun | Çözüm |
|---|---|---|---|---|---|
| DEP-01 | Orta | Bağımlılık | `OgrenciBilgiSistemi.Api/OgrenciBilgiSistemi.Api.csproj` (Swashbuckle → Microsoft.OpenApi 2.4.1) | Bilinen Yüksek önemli zafiyet (GHSA-v5pm-xwqc-g5wc); Swagger üretimde de açık olduğundan saldırı yüzeyi var | Swashbuckle'ı yükselt / `Microsoft.OpenApi`'yi doğrudan güvenli sürüme sabitle; Swagger'ı Development'a sınırla |
| DEP-02 | Orta | Bağımlılık | `OgrenciBilgiSistemi/OgrenciBilgiSistemi.csproj` (EF Core Sqlite → SQLitePCLRaw 2.1.11) | Zafiyetli native SQLite | Sqlite üretimde kullanılmıyorsa paketi kaldır; kullanılıyorsa `SQLitePCLRaw.bundle_e_sqlite3`'ü güvenli sürüme sabitle |
| TEST-01 | Orta | Mimari/Test | çözüm geneli | Hiç otomatik test yok; yetki/tenant izolasyonu gibi kritik kurallar regresyona karşı korumasız | En azından API yetki (IDOR) ve tenant izolasyonu için `WebApplicationFactory` tabanlı entegrasyon testleri |
| BLD-01 | Düşük | Derleme | MVC 26 nullable uyarısı (1.2.1) | Null-reference riskleri uyarı olarak kalmış | Kritik akışlardakileri düzelt, `TreatWarningsAsErrors` için nullable'ı kademeli aç |
| BLD-02 | Düşük | Derleme | `OgrenciBilgiSistemi.Push/PushAltyapiExtensions.cs:49` | Obsolete `GoogleCredential.FromFile` | `CredentialFactory.FromFile<ServiceAccountCredential>(…).ToGoogleCredential()` |
| DOC-01 | Düşük | Dokümantasyon | `OgrenciBilgiSistemi/CLAUDE.md`, `OgrenciBilgiSistemi.Api/CLAUDE.md`, `OgrenciBilgiSistemi.Mobil/CLAUDE.md`, `OgrenciBilgiSistemi.Shared/CLAUDE.md` | .NET 9 / MAUI 9 / eski paket sürümleri yazıyor; MVC "5 background service" diyor, kodda 6; API CLAUDE.md "arama limiti IP başına" diyor, kod global sayaç (bkz. AUTH-04) | Dokümanı güncelle |
| BLD-03 | Düşük | Derleme | `OgrenciBilgiSistemi.Mobil` (1.2.2) | MAUI 10'da obsolete `Application.MainPage` / `DisplayAlert` yoğun kullanımı | `Windows[0].Page`, `DisplayAlertAsync` |

---

## 2. AŞAMA 2 — Çekirdek Analiz

> Not — **tenant sayısı:** Repodaki `appsettings*.json` dosyalarında (MVC ve API, Development ve Production) şu an **1 okul** tanımlı. Aşağıdaki "çapraz tenant" bulguları (SEC-03, SR-01'in okullar arası boyutu, WRK-01) bugün **gizli** durumdadır; ikinci okul eklendiği anda doğrudan veri sızıntısına dönüşür. Sunucudaki gerçek yapılandırma **doğrulanmadı**.

### 2.1 Hatalar (BUG)

| ID | Öncelik | Kategori | Dosya:Satır | Sorun | Çözüm |
|---|---|---|---|---|---|
| BUG-01 | **Yüksek** | Mobil / deadlock | `OgrenciBilgiSistemi.Mobil/Services/TemelApiService.cs:322-349` (`_refreshKilidi = new(1,1)` :42) | İlk yenileyen çağıran semaforu hiç bırakmıyor (`finally` yalnızca `_aktifRefreshGorevi == null` iken Release ediyor), sonra 346'da aynı semaforu tekrar bekliyor → **kendi kendine kilitlenme**. Access token 30 dk (`Api/Controllers/KimlikDogrulamaController.cs:29`); 30 dk sonra 401 alan ilk istek ve sonraki tüm yenilemeler sonsuza dek asılı kalır. Statik kod ile kesin; saha davranışı **doğrulanmadı** | Tek kilit altında "token değişti mi" kontrolü (aşağıda) |
| BUG-02 | Orta | Başlangıç çökmesi | `OgrenciBilgiSistemi.Push/PushAltyapiExtensions.cs:47-50`; `OgrenciBilgiSistemi/Program.cs:157`; `OgrenciBilgiSistemi.Api/Program.cs:128` | Firebase kimlik dosyası var ama bozuksa `GoogleCredential.FromFile` / `FirebaseApp.Create` yakalanmadan fırlar → **MVC ve API birlikte açılmaz** (ZKTeco dahil) | try/catch + LogError, push'u pasif bırak |
| BUG-03 | Orta | Soft delete | `OgrenciBilgiSistemi/Services/Implementations/OgrenciService.cs:246-256, 303-311, 133`; `Data/AppDbContext.cs:51-52` | Global filtre açıkken "Pasif" listesi hep boş; pasif öğrenci `FindAsync` ile bulunamadığından yeniden aktifleştirilemez | Bu sorgularda `IgnoreQueryFilters()` |
| BUG-04 | Orta | NRE | `OgrenciBilgiSistemi/Controllers/DuyurularController.cs:20-21, 42` | GenelAdmin'de `KullaniciId` claim'i yok → duyuru oluşturma hep hata | Claim'i güvenli oku |
| BUG-05 | Orta | Durum makinesi | `OgrenciBilgiSistemi.Api/Services/RandevuService.cs:236-240, 256-259` | Onay/ret'te `Durum=0` şartı yok; iptal/red edilmiş randevu yeniden onaylanabiliyor, iptal tekrarlanıp her seferinde bildirim gidiyor | `AND Durum IN (...)` |
| BUG-06 | Orta | Validasyon | `OgrenciBilgiSistemi.Api/DTOs/RandevuOlusturDto.cs:15`; `RandevuService.cs:206-207` | `SureDakika` 0/negatif → çakışma kontrolü atlanıyor; geçmiş tarihli randevu | `[Range(5,240)]`, geçmiş tarihi reddet |
| BUG-07 | Orta | SMS kaybı | `OgrenciBilgiSistemi.Api/Services/YoklamaSmsBildirimService.cs:43, 182-193` | Aynı `OgrenciId` iki kez gelirse `ToDictionary` patlar, partinin tüm SMS'i gitmez; >2100 IN parametresi SqlException | `DistinctBy`, istek boyutu sınırı |
| BUG-08 | Orta | Hata sızıntısı | `OgrenciBilgiSistemi.Api/Controllers/RandevularController.cs:40`; `Api/Services/BildirimService.cs:76`, `DuyuruService.cs:113`, `RandevuService.cs:55` | `ex.Message` istemciye dönüyor; `sayfaNo<1` negatif OFFSET → 500 / SQL hata metni | `Math.Max(1, sayfaNo)`, genel hata mesajı |
| BUG-09 | Orta | Mobil çökme | `OgrenciBilgiSistemi.Mobil/App.xaml.cs:69-75`; `Views/AnaBaslikView.xaml.cs:46-82` | try/catch'siz `async void` handler'lar; `Shell.Current` null ise uygulama çöker; eşzamanlı 401'lerde çoklu navigasyon | try/catch + tek seferlik bayrak |
| BUG-10 | Orta | Mobil yenileme | `OgrenciBilgiSistemi.Mobil/Services/TemelApiService.cs:387-391, 127-131` | Refresh sırasında ağ hatası/timeout da oturumu kapatıyor | Yalnızca 400/401'de oturumu kapat |
| BUG-11 | Orta | Mobil thread-safety | `OgrenciBilgiSistemi.Mobil/Services/TemelApiService.cs:14-23, 65-77` | Paylaşılan `HttpClient.DefaultRequestHeaders.Authorization` her istekte değiştiriliyor (thread-safe değil) | Header'ı `HttpRequestMessage` üzerinde ya da `DelegatingHandler` ile ver |
| BUG-12 | Orta | Push navigasyonu | `OgrenciBilgiSistemi.Mobil/Services/BildirimYonlendirmeServisi.cs:64, 68`; `AppShell.xaml.cs:12, 15` | Global route'a `//` mutlak navigasyon + DI'da olmayan VM → bildirime tıklayınca hiçbir yere gidilmiyor (hata yutuluyor). **Doğrulanmadı** (runtime) | Göreli route + `IQueryAttributable` |
| BUG-13 | Düşük | Çeşitli | `OgrenciBilgiSistemi/Services/Implementations/DashboardService.cs:46` (ay=13 → 500); `Infrastructure/FileStorage/LocalFileStorage.cs:33-37` çağrısı transaction içinde (`OgrenciService.cs:148`, rollback'te foto kaybı); `Mobil/Services/KullaniciOturum.cs:93, 100` (`OturumYukleAsync` hiç çağrılmıyor, `RoundtripKind` yok); `Shared/Services/OkulYapilandirmaServisi.cs:16-22` (tekrarlı OkulKodu'nda açılış hatası); `Api/Services/OgretmenRandevuService.cs:146-150` (`Debug.WriteLine` ile hata yutma); `Mobil/ViewModels/GirisGorunumModel.cs:124` (şifre `Trim()`); `Api/DTOs/YoklamaKayitOgesiDto.cs:7` (DurumId aralık yok); `Api/Controllers/KimlikDogrulamaController.cs:104-114,190-192` (GenelAdmin refresh hep 401) | Küçük düzeltmeler |

**BUG-01 önce/sonra** (`TemelApiService.cs:316-350`):

```csharp
// ÖNCE
await _refreshKilidi.WaitAsync();
try {
    if (_aktifRefreshGorevi != null) { _refreshKilidi.Release(); return await _aktifRefreshGorevi; }
    _aktifRefreshGorevi = GercekTokenYenilemeAsync(refreshToken);
} finally { if (_aktifRefreshGorevi == null) _refreshKilidi.Release(); }   // ilk çağıran kilidi bırakmaz
try { return await _aktifRefreshGorevi; }
finally { await _refreshKilidi.WaitAsync(); /* kendi tuttuğu kilidi bekler → DEADLOCK */ ... }

// SONRA
private async Task<bool> TokenYenilemeAsync(string? istekteKullanilanToken)
{
    await _refreshKilidi.WaitAsync();
    try
    {
        var guncel = KullaniciOturum.YetkiToken;
        if (!string.IsNullOrEmpty(guncel) && guncel != istekteKullanilanToken)
            return true;                                   // başka istek zaten yeniledi
        var refreshToken = KullaniciOturum.RefreshToken;
        return !string.IsNullOrEmpty(refreshToken) && await GercekTokenYenilemeAsync(refreshToken);
    }
    finally { _refreshKilidi.Release(); }
}
```

**Async/yaşam süresi özeti:** Backend'de `.Result`/`.Wait()`/`async void` **yok**; DbContext singleton'da tutulmuyor; API'de bağlantılar `await using`. Arka plan servislerinin hepsi `ExecuteAsync` içinde `Exception` yakalıyor → `BackgroundServiceExceptionBehavior.StopHost` ile host çökmesi riski yok (ancak bkz. WRK-11). Memory leak: backend'de önemli sızıntı yok; ZKTeco COM nesnesi `StopAsync`'te release edilmiyor (WRK-11); mobilde yalnızca `Views/OgrenciDetayView.xaml.cs:19` `PropertyChanged` aboneliği (Düşük).

### 2.2 Kod kalitesi ve mimari (ARCH / TEST)

| ID | Öncelik | Kategori | Dosya:Satır | Sorun | Çözüm |
|---|---|---|---|---|---|
| ARCH-01 | Orta | Mimari | `OgrenciBilgiSistemi/Program.cs:91, 123-128`; `web.config:8` | ZKTeco ve 6 arka plan işi **web uygulamasının içinde**: app pool recycle/idle/deploy anında kart okuma durur; web ve donanım aynı x86 süreçte | Kısa vade: OPS-01. Orta vade: ZKTeco + arka plan işlerini ayrı x86 Windows Service'e taşı |
| ARCH-02 | Orta | Tekrar eden iş mantığı | API `Services/{RandevuService, OgretmenRandevuService, DuyuruService, BildirimService, OgrenciService}.cs` ↔ MVC `Services/Implementations/*` | Aynı kurallar iki kez (ham SQL + EF) yazılmış; iki taraf ayrışıyor (ör. pasif veli kontrolü MVC'de var, API'de yok → AUTH-05; yemek/öğle çıkış kuralı USB yolunda var, ZK yolunda yok → WRK-08) | Kuralları Shared'da saf C# olarak topla + sözleşme testleri |
| ARCH-03 | Orta | Tekrar eden kod | `Services/Implementations/KitapService.cs:143`, `KullaniciService.cs:341`, `OgretmenProfilService.cs:159`, `Infrastructure/FileStorage/LocalFileStorage.cs` | 4 ayrı dosya yükleme implementasyonu; yalnızca biri doğrulama yapıyor (→ SEC-05) | Hepsini `IFileStorage`'a indir |
| ARCH-04 | Düşük | Katman | `Controllers/KartOkuController.cs:97-339` (~230 satır iş mantığı); `ViewComponents/MenuViewComponent.cs:22-26` (doğrudan `AppDbContext`) | CLAUDE.md "controller'da DbContext yok / iş mantığı serviste" kuralına aykırı | Servise taşı |
| ARCH-05 | Düşük | Test edilebilirlik | Mobil ViewModel'ler (`GirisGorunumModel.cs:128-181` vb.) | ViewModel'ler `Application.Current.MainPage.DisplayAlert` ile doğrudan UI'a erişiyor | `IDialogService` soyutlaması |
| ARCH-06 | Düşük | Dağıtım | `OgrenciBilgiSistemi.Api/CLAUDE.md` ("x86 — tutarlılık için") | API'nin COM bağımlılığı yok ama x86 çalışıyor → 5.000 eşzamanlı veli yükünde 32-bit adres alanı (~4 GB) sınırı | API'yi ayrı x64 app pool'da çalıştır |
| (TEST-01) | – | Test | – | Bkz. 1.3 TEST-01 (tekrar sayılmadı) | – |

SOLID/DI: DI kayıtları tutarlı (Scoped servisler, Singleton'larda `IServiceScopeFactory`). Asıl sorun katman ihlalleri değil, **tenant bağlamının DI dışında (Task.Run / hosted service) kaybolması** (bkz. SEC-03, WRK-01).

### 2.3 Performans (PERF)

Senaryo: sabah giriş saatinde ~3.000 kart okutması → SMS → ~5.000 velinin aynı 10-15 dakikada uygulamayı açması. Veli istekleri: `api/ogrenciler/benim`, `api/gecis-kayit`, `api/bildirimler`, `api/duyurular/benim`.

| ID | Öncelik | Kategori | Dosya:Satır | Sorun | Çözüm |
|---|---|---|---|---|---|
| PERF-01 | Orta | Sorgu/indeks | `OgrenciBilgiSistemi.Api/Services/GecisKayitService.cs:41-64, 107-117`; MVC `Services/Implementations/GecisRaporService.cs:74, 119-132, 267, 272`; `Models/OgrenciDetayModel.cs:7-9`; `Data/AppDbContext.cs:82-95` | `ORDER BY/WHERE COALESCE(GTarih, CTarih)` sargable değil → mevcut tarih indeksleri kullanılamıyor; `(OgrenciId, tarih)` bileşik indeksi yok; tek öğrenci sorgusu tarih verilmezse **tüm geçmişi sayfalamasız** döndürüyor. Tepe saatte en sık çağrılan veli ucu | `(OgrenciId, OgrenciGTarih)` + `(OgrenciId, OgrenciCTarih)` indeks, OR'lu aralık veya persisted computed column, zorunlu sayfalama |
| PERF-02 | Orta | N+1 | `OgrenciBilgiSistemi.Api/Services/DuyuruService.cs:75-79`; `Api/Services/BildirimService.cs:32-55` | Duyuru oluştururken her veli için ayrı bağlantı + INSERT + FCM çağrısı **istek içinde**, transaction yok | Toplu INSERT, push'u kuyruğa/arka plana |
| PERF-03 | Orta | Kapasite | Tüm API servisleri (ör. `Api/Services/OgrenciService.cs:48`); connection string'ler | Her sorgu yeni `SqlConnection` (pool ile sorun değil) ama `Max Pool Size` varsayılan 100, `CommandTimeout` varsayılan; yavaş PERF-01 sorguları tepe yükte pool'u tüketir → "Timeout expired… max pool size" | Önce PERF-01; sonra pool boyutu ve kısa micro-cache (CACHE bölümü) |
| PERF-04 | Orta | Bellek | MVC `Services/Implementations/YemekhaneService.cs:~330-436` | Tüm öğrenciler + yıllık yemek kayıtları belleğe alınıp C#'ta filtrelenip sayfalanıyor | SQL'de gruplama ve sayfalama |
| PERF-05 | Orta | Excel | MVC `Controllers/OgrenciGirisCikisController.cs:252-405`; `GecisRaporService.cs:54-76`; `OgrenciService.cs:338`; `AidatService.cs:333-336` | Tarih filtresi zorunlu değil; tüm geçmiş Include'larla belleğe (x86 süreçte OutOfMemory riski) | Tarih aralığı zorunlu + üst sınır, DTO projeksiyonu |
| PERF-06 | Düşük | Sayfalama | API `OgrenciService.cs:79-117, 366-413`; `ServisService.cs:138-176`; `VeliListeService.cs:22-57`; `YoneticiService.cs:104-151` | Liste uçları sayfalamasız | Sayfalama |
| PERF-07 | Düşük | Döngüde sorgu | API `OgrenciService.cs:343-354`; `ServisService.cs:208-216` | Öğrenci başına ayrı MERGE round-trip | TVP / tek MERGE |
| PERF-08 | Düşük | Filtre maliyeti | MVC `Infrastructure/MenuYetkiAttribute.cs:63-112`; `MenuIslemYetkiAttribute.cs:52-77` | Her panel isteğinde 2-3 yetki sorgusu (menü ağacı ise cache'li, `MenuService.cs:35-37`) | Yetki sonucunu tenant+kullanıcı anahtarlı cache'le (mevcut `menu:` invalidation'ı kullan: `KullaniciService.cs:325`) |

EF tarafında: okuma sorgularında `AsNoTracking` genel olarak kullanılıyor; `ToList` sonrası filtreleme yalnızca PERF-04/05'te; ciddi N+1 MVC'de bulunmadı.

### 2.4 Güvenlik (SEC) — kimlik doğrulama dışı

| ID | Öncelik | Kategori | Dosya:Satır | Sorun | Çözüm |
|---|---|---|---|---|---|
| SEC-01 | **Yüksek** | IDOR (yazma) + sahte SMS | `OgrenciBilgiSistemi.Api/Services/ServisService.cs:192-216` (MERGE kaynağı `SELECT @ogrenciId`, :195); `Api/Controllers/ServisController.cs:89-107`; `Api/Services/YoklamaSmsBildirimService.cs:42-49, 186-193` | Servis şoförü **herhangi bir** `OgrenciId` için yoklama yazabiliyor; öğrencinin o servise ait olduğu kontrol edilmiyor → o öğrencinin velisine sahte "Bindi/Binmedi" SMS'i gider, veli geçmişinde görünür | MERGE kaynağını `Ogrenciler WHERE ServisId=@kullaniciId` ile kısıtla, SMS'i yalnızca yazılanlara gönder |
| SEC-02 | **Yüksek** | KVKK / aşırı veri | `OgrenciBilgiSistemi.Api/Services/OgrenciService.cs:420-471`; `Api/Controllers/OgrencilerController.cs:167-186, 403-412`; `Api/Services/GecisKayitService.cs:55, 109`; `Mobil/Views/OgrenciDetayView.xaml:41` | `/ogrenciler/{id}/details`: veli adresi, e-posta, meslek, iş yeri, telefon ve **kart UID'si**. Öğretmen okuldaki **tüm** öğrencilerinkini, şoför kendi servisindekileri alıyor; kart no geçiş kayıtlarında da dönüyor | Rol bazlı projeksiyon; kart no'yu mobil yanıtlardan tamamen kaldır |
| SEC-03 | **Yüksek** (çok okulda) | Çapraz tenant veri / SMS | `OgrenciBilgiSistemi/Controllers/KartOkuController.cs:306-312`; `Services/BackgroundServices/KartOkumaOlayIsleyiciService.cs:87-93`; `Program.cs:45-57`; `Services/Implementations/SmsGonderimService.cs:36-40, 76-88` | Fire-and-forget SMS yeni DI scope'unda çalışıyor, `TenantBaglami` boş → **ilk okulun DB'si**. B okulundaki öğrencinin adıyla A okulunda aynı `OgrenciId`'nin velisine SMS gider; A'daki başka bir kaydın `SmsGonderildi` bayrağı set edilir | Tenant'ı Task'a kopyala; fallback'i kaldır (aşağıda) |
| SEC-04 | **Yüksek** | Dosya silme (path traversal) | `Models/OgrenciModel.cs:68` + `Services/Implementations/OgrenciService.cs:107-113, 148` → `Infrastructure/FileStorage/LocalFileStorage.cs:33-37`; `ViewModels/KitapFormVm.cs:47` + `KitapService.cs:95-100, 119, 153-166` | `OgrenciGorsel`/`KitapGorsel` formdan bind ediliyor ve saklanıyor; sonraki görsel güncellemesinde `File.Delete(wwwroot + existingPath)` → `../appsettings.json`, `../web.config` silinebilir (uygulama çöker) | İstemci yolunu yok say; silmeden önce `GetFullPath` ile `uploads` altında olduğunu doğrula |
| SEC-05 | **Yüksek** | Doğrulanmamış upload / kalıcı XSS | MVC `Services/Implementations/KitapService.cs:143-181`; (Orta: `KullaniciService.cs:341-355`, `OgretmenProfilService.cs:159-173` — AdminOnly) | Uzantı/magic-byte/boyut kontrolü yok; `.html`/`.svg` `/uploads/` altına yazılıp aynı origin'den sunuluyor (`Program.cs:175`, API `Program.cs:153-160`) → Admin açınca script onun oturumuyla işlem yapar | Tüm yüklemeleri `LocalFileStorage`'a yönlendir; uploads'a `X-Content-Type-Options: nosniff` |
| SEC-06 | **Yüksek** | Aktarım güvenliği | `OgrenciBilgiSistemi.Mobil/Constants.cs:7, 10`; `Mobil/appsettings.json:2`; `Platforms/Android/AndroidManifest.xml:7` (`usesCleartextTraffic="true"`); `Platforms/iOS/Info.plist:61-65` (`NSAllowsArbitraryLoads`); `ViewModels/GirisGorunumModel.cs:50, 152`; `OkulSecimGorunumModel.cs:66` | Okul kayıt listesi ve fallback API adresi **`http://`** (fallback ham IP:port). Kayıt listesinden gelen `ApiUrl` şema kontrolü olmadan kullanılıyor. Aynı ağdaki saldırgan (okul/kafe Wi-Fi) okul listesini değiştirip **login isteğini (şifre dahil) kendi sunucusuna** yönlendirebilir; JWT ve öğrenci verisi düz metin | Her yerde HTTPS, cleartext/ATS istisnalarını kaldır, `ApiUrl` için https zorunlu |
| SEC-07 | **Yüksek** | Secret / git geçmişi | Geçmiş: `c8a518d` … `334d337~1` arası `OgrenciBilgiSistemi.Api/appsettings.json` (`SmsAyarlari:KullaniciAdi`, `SmsAyarlari:Sifre` dolu), `OgrenciBilgiSistemi/appsettings.json`, `Api/appsettings.Development.json` (SQL şifresi, dev JWT anahtarı). Bugün diskte: MVC `appsettings.json:18` `GenelAdmin:SifreHash`, `:23` `SmsAyarlari:Sifre`; API `appsettings.json:29, 34` | `334d337` dosyaları gitignore'a aldı ama **geçmiş temizlenmedi** ve remote'ta. Alt ajan SHA-256 karşılaştırmasıyla **SMS şifresinin ve GenelAdmin hash'inin bugün hâlâ aynı** olduğunu tespit etti (değerler bu rapora yazılmadı). Repo görünürlüğü (public/private) **doğrulanmadı** | 1) ilksms şifresini değiştir 2) GenelAdmin şifresini değiştir 3) geçmişteki SQL şifresi kullanılıyorsa değiştir 4) değerleri ortam değişkenine taşı 5) isteğe bağlı `git filter-repo` |
| SEC-08 | Orta | Secret yönetimi | API `appsettings.Production.json:6, 10` (`Jwt:SecretKey`, SQL şifresi); tüm connection string'ler `TrustServerCertificate=True` | Versiyonda değil ama JWT secret dosyada (CLAUDE.md kuralına aykırı) ve publish ile kopyalanıyor; SQL sertifikası doğrulanmıyor | `Jwt__SecretKey` ve connection string'i IIS ortam değişkenine; SQL sertifikası |
| SEC-09 | Orta | API yüzeyi / anonim dosya | API `Program.cs:135-139, 153-160`; MVC `Program.cs:175` | Swagger prod'da açık (ve DEP-01 zafiyetli paket); HSTS yok (API); `wwwroot/uploads` (öğrenci fotoğrafları) **kimlik doğrulamasız**. Yeni dosya adları GUID (`LocalFileStorage.cs:42`), eski adlar **doğrulanmadı**; foto URL'leri SR-01 ile diğer oturumlara da gidiyor | Swagger'ı `IsDevelopment` ile sınırla, `UseHsts`, fotoğrafları yetkili endpoint'ten sun |
| SEC-10 | Orta | IDOR (okuma) | `OgrenciBilgiSistemi.Api/Controllers/DuyurularController.cs:48-54`; `Api/Services/DuyuruService.cs:228-255` | `GET duyurular/{id}` erişim kontrolü yok; ardışık Id ile başka sınıfların duyuruları okunur | `OkunduIsaretle`'deki (DuyuruService.cs:141-154) erişim koşulunu uygula |
| SEC-11 | Orta | Yetki (iş kuralı doğrulanmadı) | `Api/Controllers/OgrencilerController.cs:84-130, 287-380`; `Api/Services/OgrenciService.cs:269-364` | Her öğretmen her sınıfın listesini/yoklamasını okuyup **yazabiliyor** ve velilere devamsızlık SMS'i tetikleyebiliyor. Branş öğretmeni için tasarım gereği olabilir | Öğretmen–sınıf/ders atamasına göre kısıtla |
| SEC-12 | Orta | KVKK | MVC `Controllers/ZiyaretcilerController.cs:225-232`; `Services/Implementations/ZiyaretciService.cs:197-212` | GET `KartOkut?kartNo=` ziyaretçinin TC, telefon, adresini JSON döndürüyor; yalnızca menü yetkisine bağlı | Hassas alanları çıkar/maskele + `MenuIslemYetki` |
| SEC-13 | Orta | Reflected XSS / open redirect | MVC `Controllers/AidatController.cs:142` → `Views/Aidat/Ozet.cshtml:104-105` | `returnUrl=javascript:...` `href`'e yazılıyor | `Url.IsLocalUrl` değilse null |
| SEC-14 | Orta | KVKK / log | `Services/Implementations/ZKTecoService.cs:200-202, 223`; `KartOkumaOlayIsleyiciService.cs:61, 137`; `SmsGonderimService.cs:92-93` (telefon); `OgretmenProfilService.cs:86`; `ZiyaretciService.cs:37`; `AidatController.cs:283` (`{@Dto}`); MVC `web.config:8` (`stdoutLogEnabled="true"`) | Kart UID, veli telefonu, ad-soyad Information/Warning seviyesinde loglanıyor ve prod'da **stdout dosyasına** (rotasyonsuz) yazılıyor — CLAUDE.md yasağının ihlali | Maskele / yalnızca Id logla; stdout'u kapat, yapılandırılmış log (OPS-02) |
| SEC-15 | Orta | Cihaz güvenliği | `Models/CihazModel.cs:34-45`; `SetCommPassword` kodda yok | ZKTeco cihazları parolasız (CommKey yok); yerel ağdan 4370 portuna erişen herkes log okuyup silebilir, kullanıcı ekleyebilir. **Biyometrik veri aktarılmıyor**: yalnızca `SetUserInfo(OgrenciId, AdSoyad, boş şifre)` + `SetStrCardNumber` (`CihazService.cs:103-117`); template okuma kodu yok, `ClearData(5)` cihazdaki template'leri siliyor (`CihazService.cs:562-567`) | Cihaz başına CommKey (şifreli saklama), ayrı VLAN |
| SEC-16 | Orta | Mobil | `Mobil/Services/GuncellemeKontrolService.cs:19, 25, 47` | HTTP üzerinden gelen `storeUrl` doğrulanmadan `Launcher.OpenAsync` ile açılıyor (SEC-06 ile phishing) | Host allowlist |
| SEC-17 | Orta | Mobil kimlik saklama | `Mobil/ViewModels/GirisGorunumModel.cs:96-98, 198-199`; `Views/AnaBaslikView.xaml.cs:125-135` | "Beni Hatırla" **düz şifreyi** SecureStorage'da tutuyor, logout silmiyor (iOS Keychain'de uygulama silinse de kalır). Token'lar doğru şekilde SecureStorage'da (`KullaniciOturum.cs:58-82`) | Şifre saklama; yalnızca kullanıcı adını hatırla |
| SEC-18 | Orta | Mobil | `Mobil/Platforms/Android/AndroidManifest.xml:4` | `allowBackup="true"` | `false` veya `dataExtractionRules` |
| SEC-19 | Orta | Service account | API `appsettings.json:42`; MVC `appsettings.json:31` (`..\firebase-adminsdk.json`) | Göreli yol uygulama klasörünün üstüne işaret ediyor; IIS'te web köküne denk gelirse indirilebilir. **Doğrulanmadı** | Web kökü dışı mutlak yol / `FIREBASE_CREDENTIALS_PATH` |
| SEC-20 | Düşük | Çeşitli | API `KimlikDogrulamaController.cs:64-74` (`hash-uret?sifre=` — şifre query string'de, IIS loglarına düşer); `Api/Controllers/CihazlarController.cs:36-47` (başkasının FCM token'ı biliniyorsa silinebilir); `RandevularController.cs:170-192` (keyfi kişinin dolu saatleri); MVC `Program.cs:74-79, 137-144` (cookie `SecurePolicy`/`SameSite` açık değil); `KartOkuController.cs:97-99` (`IgnoreAntiforgeryToken`); `BekleyenSmsRetryService.cs:129-142`, `SmsService.cs:73` (SMS metni+telefon süresiz, `ex.ToString()` DB'de); `Mobil/MauiProgram.cs:136-144` (Firebase iOS istemci anahtarı kodda — public kabul edilir, bundle ID'ye kısıtla); `Mobil/Services/KullaniciOturum.cs:156-159` (logout sonrası push bilinçli olarak devam — paylaşılan cihazda KVKK) | Küçük düzeltmeler |

**SQL injection:** Bulunmadı. API'de dinamik SQL parçaları yalnızca sabit whitelist'ler (`Api/Services/OgrenciService.cs:15-19`), IN listeleri `@idN` parametreli (`YoklamaSmsBildirimService.cs:182-193`), MVC'de tek `ExecuteSqlRaw` parametreli (`YemekhanePollingService.cs:279-280`). `LIKE` jokerleri escape edilmiyor (`Api/Services/GirisService.cs:187`) → injection değil ama `q=%%%` ile tüm kullanıcı adları listelenir (AUTH-04).
**XSS:** `Html.Raw` yok; SignalR istemcisi `textContent`/`sanitizeHtml` kullanıyor (`wwwroot/js/signalr-handler.js:241-392`). Açıklar: SEC-05, SEC-13.
**CSRF:** MVC POST'ların tamamı `[ValidateAntiForgeryToken]` veya sınıf düzeyi `AutoValidateAntiforgeryToken` taşıyor; tek istisna `KartOku/UsbKartOkundu` (SEC-20).
**CORS:** Güvenli varsayılan (`Api/Program.cs:40-57`), `AllowCredentials` yok.
**Şifre saklama:** `PasswordHasher` (Identity v3, PBKDF2) — iyi (`KullaniciService.cs:62`); `SuccessRehashNeeded` ele alınmıyor (Düşük).

**Yüksek güvenlik bulguları — önce/sonra:**

SEC-01 (`Api/Services/ServisService.cs:195`):
```sql
-- ÖNCE
USING (SELECT @ogrenciId AS OgrenciId) AS source
-- SONRA: yalnızca şoförün kendi servisindeki aktif öğrenci
USING (SELECT O.OgrenciId FROM Ogrenciler O
       WHERE O.OgrenciId = @ogrenciId AND O.ServisId = @kullaniciId AND O.IsDeleted = 0) AS source
```
```csharp
// SONRA (C#): yazılmayanlar için SMS tetiklenmez
var yazilanlar = new List<YoklamaKayitOgesiDto>();
foreach (var kayit in yoklamaVerisi.DistinctBy(k => k.OgrenciId))
{
    ...
    if (await cmd.ExecuteNonQueryAsync() > 0) yazilanlar.Add(kayit);
}
await transaction.CommitAsync();
return yazilanlar;   // ServisController SMS'i yalnızca bunlar için başlatır
```

SEC-02 (`Api/Controllers/OgrencilerController.cs:176-179`):
```csharp
// ÖNCE
var yetkiSonucu = OgrenciyeErisimKontrol(detaylar.VeliId, detaylar.ServisId);
if (yetkiSonucu != null) return yetkiSonucu;
return Ok(detaylar);
// SONRA — rol bazlı projeksiyon (kalıcı çözüm: rol başına ayrı DTO)
var yetkiSonucu = OgrenciyeErisimKontrol(detaylar.VeliId, detaylar.ServisId);
if (yetkiSonucu != null) return yetkiSonucu;
var rol = User.FindFirst("rol")?.Value;
detaylar.OgrenciKartNo = null;                                     // hiçbir mobil role dönmez
if (rol is "Ogretmen" or "Servis")
    detaylar.VeliAdres = detaylar.VeliEmail = detaylar.VeliMeslek = detaylar.VeliIsYeri = null;
return Ok(detaylar);
```

SEC-03 (`Controllers/KartOkuController.cs:306-312` ve `Program.cs:45-57`):
```csharp
// ÖNCE
_ = Task.Run(async () =>
{
    using var smsScope = _scopeFactory.CreateScope();          // TenantBaglami boş → ilk okul
    var smsSvc = smsScope.ServiceProvider.GetRequiredService<ISmsGonderimService>();
    await smsSvc.GecisSmsBildir(ogrId, ogrAdSoyad, gecisTipiKopya, anlikZaman);
});
// SONRA
var (okulKodu, cs, okulAdi) = (_tenant.OkulKodu, _tenant.ConnectionString, _tenant.OkulAdi);
_ = Task.Run(async () =>
{
    using var smsScope = _scopeFactory.CreateScope();
    var t = smsScope.ServiceProvider.GetRequiredService<TenantBaglami>();
    t.OkulKodu = okulKodu; t.ConnectionString = cs; t.OkulAdi = okulAdi;   // DbContext çözülmeden ÖNCE
    var smsSvc = smsScope.ServiceProvider.GetRequiredService<ISmsGonderimService>();
    await smsSvc.GecisSmsBildir(ogrId, ogrAdSoyad, gecisTipiKopya, anlikZaman);
});

// Program.cs — fallback kaldırılır (WRK-01 düzeltildikten SONRA)
else
    throw new InvalidOperationException("TenantBaglami boş: DbContext tenant olmadan oluşturulamaz.");
```

SEC-04 (`Infrastructure/FileStorage/LocalFileStorage.cs:33-37`; aynı desen `KitapService.cs:153-166`):
```csharp
// ÖNCE
var old = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingPath.TrimStart('/'));
if (File.Exists(old)) File.Delete(old);
// SONRA
var uploadsRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads")) + Path.DirectorySeparatorChar;
var old = Path.GetFullPath(Path.Combine(_env.WebRootPath, existingPath.TrimStart('/', '\\')));
if (old.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(old))
    File.Delete(old);
// + OgrenciService.EkleAsync / KitapService.AddAsync başında: model.OgrenciGorsel = null; model.KitapGorsel = null;
```

SEC-05 (`Services/Implementations/KitapService.cs:170-178`):
```csharp
// ÖNCE
var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);   // her uzantı kabul
using (var stream = new FileStream(newPath, FileMode.Create)) await file.CopyToAsync(stream, ct);
// SONRA
kitap.KitapGorsel = await _files.SaveImageAsync(gorsel, kitap.KitapGorsel, ct);  // IFileStorage: jpg/png + magic byte + 2MB
```

SEC-06 (`Mobil/Constants.cs:7,10`, manifest/plist, `GirisGorunumModel.cs:50,152`):
```csharp
// ÖNCE
public const string KayitSunucuUrl  = "http://<host>/okullar.json";
public const string VarsayilanApiUrl = "http://<ip>:<port>/api/";
// SONRA
public const string KayitSunucuUrl  = "https://<host>/okullar.json";
public const string VarsayilanApiUrl = "https://<host>/api/";
static bool GuvenliUrlMi(string url) =>
    Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps;
if (!GuvenliUrlMi(secilenOkul.ApiUrl)) { /* uyarı, girişi engelle */ return; }
```
```xml
<!-- ÖNCE --> android:allowBackup="true" android:usesCleartextTraffic="true"
<!-- SONRA --> android:allowBackup="false" android:usesCleartextTraffic="false"
<!-- iOS Info.plist: NSAppTransportSecurity / NSAllowsArbitraryLoads bloğu kaldırılır -->
```

SEC-07: Kod değişikliği değil, operasyon: kimlik bilgisi rotasyonu → ortam değişkeni:
```json
// SONRA (appsettings.json'da boş; değerler IIS ortam değişkeninden)
"SmsAyarlari": { "KullaniciAdi": "", "Sifre": "" },
"GenelAdmin":  { "KullaniciAdi": "", "SifreHash": "" }
// SmsAyarlari__Sifre=<REDACTED>, GenelAdmin__SifreHash=<REDACTED>, Jwt__SecretKey=<REDACTED>
```

### 2.5 DTO / Entity

| ID | Öncelik | Kategori | Dosya:Satır | Sorun | Çözüm |
|---|---|---|---|---|---|
| DTO-01 | (AUTH-02 ile birleştirildi) | Over-posting | → bkz. **AUTH-02** (`ViewModels/OgrenciVeliFormVm.cs:8` entity'yi doğrudan bind ediyor) | Entity bind + `Add(model)` tüm nav grafını ekliyor | Form DTO'su |
| DTO-02 | Düşük | Entity dönüşü | API `Services/OgrenciService.cs`, `ServisService.cs:45-52`, `KimlikDogrulamaController.cs:152-162` | Ham `OgrenciModel`/login yanıtında `IsDeleted` vb. dönüyor (API CLAUDE.md kuralına aykırı) | Uç bazlı DTO |
| DTO-03 | Düşük | Mobil uyumu | `Mobil/Models/VeliDetay.cs:17-23` (YakinlikTipi 5=Diğer eksik); `Mobil/Models/VeliProfil.cs:8-9`, `Mobil/Models/Ogretmen.cs:7-11` (API'de olmayan alanlar, modeller kullanılmıyor); `Mobil/Models/SinifYoklama.cs` (Shared DTO'nun kopyası) | Küçük sapmalar; 12 model + 8 istek DTO'su karşılaştırıldı, **kırıcı uyumsuzluk yok**. OgrenciDetay/Randevu/Duyuru/Bildirim `Shared/ApiContracts` üzerinden ortak | Ölü modelleri sil, Shared DTO'yu kullan |

---

## 3. AŞAMA 3 — Rol Bazlı Yetkilendirme

### 3.1 Rol modeli

- Roller (`OgrenciBilgiSistemi.Shared/Enums/KullaniciRolu.cs`): Admin, Ogretmen, Servis, Veli, GenelAdmin(=5). `Kullanicilar.Rol` **tek değer** → çoklu rol (hem öğretmen hem veli) **desteklenmiyor**; aynı kişi iki ayrı hesap kullanmak zorunda.
- API claim adı `"rol"` (`Api/Controllers/KimlikDogrulamaController.cs:279-296`), MVC `ClaimTypes.Role` (`Controllers/HesaplarController.cs:70, 120`). Yazımlar tutarlı ("Ogretmen", "Servis"; "Öğretmen" varyantı yok).
- Policy: yalnızca `AdminOnly` (MVC `RequireRole`, API `RequireClaim("rol", …)`); diğer tüm rol kontrolleri action içinde `if (rol != "...")` şeklinde elle yazılmış → unutulmaya açık.
- API'de global `FallbackPolicy` yok ama tüm controller'larda sınıf düzeyi `[Authorize]` var; **unutulmuş action bulunmadı**.
- **İstemci tarafı gizleme:** Mobilde admin ekranları `yonetici/*` (AdminOnly) ile, yoklama yazımı rol kontrolüyle sunucuda korunuyor. MVC menü gizleme `MenuYetkiFilter` ile sunucuda da uygulanıyor ancak **controller düzeyinde** (AUTH-03).

### 3.2 Rol × endpoint matrisi (API — mobil)

✅ = izinli (kendi verisi) · 🌐 = izinli (okul geneli) · ❌ = sunucu reddeder · ⚠️ = izinli ve **olmamalı**

| Endpoint | Anonim | Veli | Öğretmen | Servis | Admin | GenelAdmin |
|---|---|---|---|---|---|---|
| `GET kimlik-dogrulama/okullar` | 🌐 (okul listesi enumerate) | | | | | |
| `POST kimlik-dogrulama/login`, `/refresh` | ⚠️ rate limit yok (AUTH-04) | | | | | |
| `GET kimlik-dogrulama/kullanici-ara` | ⚠️ kullanıcı adı enumerate | | | | | |
| `GET kimlik-dogrulama/hash-uret?sifre=` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| `POST kimlik-dogrulama/sifre-degistir` | ❌ | ⚠️ eski şifre sorulmuyor | ⚠️ | ⚠️ | ⚠️ | 404 |
| `GET uygulama-versiyon` | 🌐 | | | | | |
| `GET ogrenciler/benim` | ❌ | ✅ | – | ✅ | – | – |
| `GET ogrenciler/tumu`, `class/{id}`, `sinif-yoklama/{id}`, `siniflar/all-with-count` | ❌ | ❌ | 🌐 (SEC-11) | ❌ | 🌐 | 🌐 |
| `GET ogrenciler/{id}`, `/haftalik-yoklama` | ❌ | ✅ | 🌐 | ✅ | 🌐 | 🌐 |
| `GET ogrenciler/{id}/details` | ❌ | ✅ | ⚠️ 🌐 + aşırı veri (SEC-02) | ⚠️ aşırı veri | 🌐 | 🌐 |
| `POST/PUT/DELETE ogrenciler` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| `GET/POST ogrenciler/attendance/...` | ❌ | ❌ | ⚠️ tüm sınıflar (SEC-11) | ❌ | ❌ | ❌ |
| `GET gecis-kayit`, `gecis-kayit/{ogrenciId}` | ❌ | ✅ | 🌐 | ✅ | 🌐 | 🌐 |
| `GET servisler/{servisId}/...` | ❌ | ❌ | ❌ | ✅ (yalnızca kendi) | ❌ | ❌ |
| `POST servisler/yoklama-kaydet` | ❌ | ❌ | ❌ | ⚠️ **herhangi öğrenci** (SEC-01) | ❌ | ❌ |
| `GET servisler/ogrenci/{id}/yoklama-gecmisi` | ❌ | ✅ | 🌐 | ✅ | 🌐 | 🌐 |
| `GET/PUT bildirimler/*` | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `POST duyurular` | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| `GET duyurular/{id}` | ❌ | ⚠️ IDOR (SEC-10) | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| `GET duyurular/benim`, okundu uçları | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `randevular/*` (benim, oluştur, onayla, reddet, iptal) | ❌ | ✅ (taraf kontrolü var) | ✅ | ❌ | ❌ | ❌ |
| `GET randevular/cakisma-kontrolu` | ❌ | ⚠️ keyfi kişi (SEC-20) | ⚠️ | ❌ | ❌ | ❌ |
| `ogretmen-randevu/*` | ❌ | slotlar 🌐 | ✅ | slotlar 🌐 | slotlar 🌐 | slotlar 🌐 |
| `GET ogretmenler/aktif` | ❌ | 🌐 (Id+Ad) | 🌐 | 🌐 | 🌐 | 🌐 |
| `GET ogretmenler/{id}`, `yonetici/*`, `POST cihazlar/test-push` | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| `POST cihazlar/kaydet`, `DELETE kaydi-sil`, `POST token-yenile` | ❌ | ✅ (sahiplik yok, SEC-20) | ✅ | ✅ | ✅ | ✅ |
| `/swagger`, `/uploads/*` (statik) | ⚠️ 🌐 (SEC-09) | | | | | |

### 3.3 Rol × endpoint matrisi (MVC paneli)

Global: `AuthorizeFilter` → `MenuYetkiFilter` (controller düzeyi, Admin/GenelAdmin atlar) → `TenantZorunluFilter`. MVC girişinde **rol kısıtı yok**: Veli/Servis/Öğretmen de panele girebilir (`Controllers/HesaplarController.cs:104-131`).

| Controller | Koruma | Menüsüz Öğretmen/Veli/Servis | O controller'da **herhangi bir** menüsü olan kullanıcı | Admin | GenelAdmin |
|---|---|---|---|---|---|
| Hesaplar (Giris/YetkisizGiris) | AllowAnonymous | 🌐 | – | ✅ | ✅ |
| Home (dashboard) | menüden muaf | ⚠️ okul geneli sayılar | – | ✅ | ✅ |
| OkulSecim | `Roles=GenelAdmin` | ❌ | ❌ | ❌ (ama AUTH-01 ile ⚠️) | ✅ |
| Kullanicilar, Ogretmenler, Veliler, Servisler, Randevular, OgretmenRandevu, Duyurular | AdminOnly | ❌ | ❌ | ✅ (Kullanicilar'da ⚠️ GenelAdmin rolü atayabilir) | ✅ |
| Aidat | Menü + yazmada `MenuIslemYetki` | ❌ | okuma ✅, yazma ❌ (doğru desen) | ✅ | ✅ |
| Ogrenciler | Menü (controller) | ❌ | ⚠️ ekle/sil/toplu cihaz gönder + **over-post ile Admin oluşturma** (AUTH-02, AUTH-03) | ✅ | ✅ |
| Yemekhane | Menü (controller) | ❌ | ⚠️ ödeme ekle/sil (rapor menüsüyle) | ✅ | ✅ |
| Ziyaretciler | Menü (controller) | ❌ | ⚠️ sil + TC/telefon sorgulama (SEC-12) | ✅ | ✅ |
| Cihazlar | Menü (controller), AdminOnly değil | ❌ | ⚠️ `TumKullanicilariSil` | ✅ | ✅ |
| Kitaplar, KitapDetaylar | Menü (controller) | ❌ | ⚠️ doğrulanmamış upload (SEC-05), dosya silme (SEC-04) | ✅ | ✅ |
| Birimler, OgrenciGirisCikis, KartOku | Menü (controller) | ❌ | ✅ | ✅ | ✅ |
| Hub `/kartOkuHub` | Yalnızca FallbackPolicy (oturum) | ⚠️ tüm yayınları alır + sahte yayın yapar (SR-01/02) | ⚠️ | ⚠️ | ⚠️ |
| `/keep-alive` | RequireAuthorization | ✅ | ✅ | ✅ | ✅ |

### 3.4 `[Authorize]` / policy olmayan uçlar

| Uç | Proje | Değerlendirme |
|---|---|---|
| `GET api/kimlik-dogrulama/okullar` | API | Tasarım gereği; okul kodlarını enumerate ettirir |
| `POST api/kimlik-dogrulama/login`, `/refresh` | API | Tasarım gereği; **rate limit yok** (AUTH-04) |
| `GET api/kimlik-dogrulama/kullanici-ara` | API | Anonim kullanıcı adı / ad-soyad listesi (AUTH-04) — kaldırılmalı |
| `GET api/uygulama-versiyon` | API | Sorun yok |
| `/swagger/*` | API | Prod'da kapatılmalı (SEC-09) |
| API statik dosyalar (MVC `wwwroot`, `uploads` dahil) | API | Anonim öğrenci fotoğrafı (SEC-09) |
| `GET/POST Hesaplar/Giris`, `Hesaplar/YetkisizGiris` | MVC | Tasarım gereği; lockout yok (AUTH-04) |
| MVC `UseStaticFiles` (`uploads` dahil) | MVC | Anonim (SEC-09) |
| `/kartOkuHub` | MVC | Açık `[Authorize]` yok, yalnızca FallbackPolicy → her oturum sahibi bağlanır (SR-01) |

### 3.5 AUTH bulguları

| ID | Öncelik | Kategori | Dosya:Satır | Sorun | Çözüm |
|---|---|---|---|---|---|
| AUTH-01 | **Yüksek** | Yetki yükseltme / çapraz tenant | `Views/Shared/_KullaniciFormPartial.cshtml:48-49`; `Services/Implementations/KullaniciService.cs:62-73, 88`; `Controllers/HesaplarController.cs:120`; `Infrastructure/MvcTenantCozumleyiciMiddleware.cs:25-28`; `Controllers/OkulSecimController.cs:9, 30-40`; `Shared/Enums/KullaniciRolu.cs:18` | Okul Admin'i Kullanıcı formunda **Rol = Genel Yönetici (5)** seçebiliyor; sunucu reddetmiyor. Login DB rolünü claim'e yazıyor (`ClaimTypes.Role = "GenelAdmin"`), middleware tenant'ı session'dan okuyor, OkulSec her okulu seçtiriyor → **tüm okulların DB'lerinde tam yetki** | Serviste rolü doğrula; DB'den gelen GenelAdmin'i login'de reddet; GenelAdmin'i config kaynaklı ayrı claim ile işaretle |
| AUTH-02 | **Yüksek** | Over-posting → Admin hesabı | `ViewModels/OgrenciVeliFormVm.cs:8`; `Models/OgrenciModel.cs:34, 49, 65, 77-90`; `Services/Implementations/OgrenciService.cs:97-114` | Form `OgrenciModel`'i doğrudan bind ediyor; `[ValidateNever]` yalnızca doğrulamayı kapatır, bind'ı değil. `Ogrenci.Veli.KullaniciAdi`, `Ogrenci.Veli.Sifre=<çevrimdışı üretilmiş PasswordHasher v3 hash>`, `Ogrenci.Veli.Rol=1` alanları eklenerek `_db.Ogrenciler.Add(model)` ile **yeni bir Admin (veya GenelAdmin) hesabı** oluşturulur. Ogrenciler controller'ında **herhangi bir menü** (ör. "Öğrenci Veli Raporu") yeterli (AUTH-03) | Skaler form DTO'su; serviste entity'yi alan alan kur. Geçici: nav property'lere `[BindNever]` |
| AUTH-03 | **Yüksek** | Yetki granülerliği | `Infrastructure/MenuYetkiAttribute.cs:95-117`; menü seed `Data/AppDbContext.cs:596-617` (Id 20, 22, 23) | Menü yetkisi action değil **controller** düzeyinde; "rapor" menüsü verilen kullanıcı aynı controller'daki tüm yazma işlemlerini yapabiliyor (Ogrenciler, Yemekhane ödemeleri, Ziyaretciler, Cihazlar, Kitaplar) | Yazma action'larına `[MenuIslemYetki(ctrl,"Index")]` (Aidat'taki mevcut desen, `AidatController.cs:257`) |
| AUTH-04 | **Yüksek** | Brute force / enumeration | API `Controllers/KimlikDogrulamaController.cs:76-78, 170-172, 210-227`; `Api/Program.cs:103-112`; `Api/Services/GirisService.cs:183-188`; `Api/DTOs/ApiGirisIstegiDto.cs:11`; MVC `Controllers/HesaplarController.cs:36-131`; `Controllers/KullanicilarController.cs:38, 114` | Login/refresh'te (API ve MVC) rate limit ve hesap kilitleme **yok**; minimum şifre **4 karakter**; anonim `kullanici-ara` `LIKE` jokerleriyle tüm kullanıcı adlarını (= ad soyad) listeliyor; "arama" limiter'ı bölümlenmemiş → **tüm istemciler için tek sayaç** (bir saldırgan herkesin aramasını kilitler). GenelAdmin tek şifreyle tüm okullara giriyor ve aynı yüzeyden (üstelik mobil HTTP üzerinden, SEC-06) denenebiliyor | IP bazlı `PartitionedRateLimiter` (login, refresh, arama), başarısız deneme sayacı + kilit, min 8 karakter, `kullanici-ara`'yı kaldır |
| AUTH-05 | **Yüksek** | Pasif hesap erişimi | `OgrenciBilgiSistemi.Api/Services/GirisService.cs:42-45, 112-115`; MVC `Services/Implementations/VeliProfilService.cs:112` | MVC'de veli/servis "silme" yalnızca profil `IsDeleted=1`; API login ve refresh yalnızca **öğretmen** profilini kontrol ediyor → pasife alınan veli (ör. velayet değişikliği) ve şoför mobilden giriş yapıp çocuk verisini görmeye devam eder | Login ve refresh sorgusuna veli/servis profil koşulu |
| AUTH-06 | **Yüksek** | Oturum deposu | `OgrenciBilgiSistemi.Api/Services/RefreshTokenService.cs:11, 26-35, 66-74`; `Api/Program.cs:88` | Bellek içi `ConcurrentDictionary`: (1) token'lar yalnızca `KullaniciId` ile siliniyor → farklı okullarda aynı Id'li kullanıcılar birbirinin oturumunu düşürür (çok okulda); GenelAdmin Id=0 ortak; (2) kullanıcı başına tek token → ikinci cihaz birinciyi düşürür; (3) app pool recycle/deploy'da **tüm oturumlar düşer**, çoklu süreçte paylaşılmaz; (4) süresi dolanlar temizlenmez; (5) şifre değişince iptal edilmez | Anahtar `(okulKodu, kullaniciId, cihaz)`; token hash'ini tenant DB'sinde sakla; şifre değişiminde iptal |
| AUTH-07 | Orta | Panel erişimi | MVC `Controllers/HesaplarController.cs:104-131`; `Infrastructure/MenuYetkiAttribute.cs:52-58` | Veli/Servis mobil şifreleriyle panele girip dashboard'da okul geneli sayıları görüyor, SignalR'a bağlanabiliyor | Panel girişini Admin/Öğretmen ile sınırla |
| AUTH-08 | Orta | Oturum iptali | MVC `Program.cs:136-144` | Cookie'de `OnValidatePrincipal` yok; silinen / rolü düşürülen kullanıcı cookie yaşadıkça (8 saat **sliding** → aktif kullanımda süresiz) erişimini korur | `OnValidatePrincipal` ile IsDeleted/Rol (security stamp) kontrolü |
| AUTH-09 | Orta | Hesap ele geçirme | API `Controllers/KimlikDogrulamaController.cs:230-253`; `Api/Services/GirisService.cs:152-175` | Şifre değişiminde eski şifre istenmiyor; çalınan 30 dk'lık access token kalıcı ele geçirmeye dönüşür; refresh token'lar iptal edilmez | Mevcut şifreyi doğrula + token iptali |
| AUTH-10 | Orta | Logout | `Mobil/Services/KullaniciOturum.cs:202-221`; API `KimlikDogrulamaController.cs:255-266` | Logout `[Authorize]` + süresi dolmuş access token ile gidiyor → 401, sessizce yutuluyor; sunucudaki refresh token 8 saat geçerli kalıyor | Logout'u refresh token ile `[AllowAnonymous]` yap |
| AUTH-11 | Düşük | Çeşitli | API `KimlikDogrulamaController.cs:29, 309` (pasifleştirme sonrası access token ≤30 dk geçerli — refresh DB'den kontrol ediyor, :190-192, kabul edilebilir); `Api/Services/GirisService.cs:67, 133` (Rol NULL → Öğretmen sayılıyor); MVC `KullanicilarController` (okul Admin'i GenelAdmin satırını düzenleyebiliyor) | – |

**JWT yapılandırması:** HS256 simetrik anahtar, issuer/audience/lifetime/signing key doğrulanıyor (`Api/Program.cs:65-74`), access 30 dk, refresh 8 saat tek kullanımlık (rotasyonlu, `RefreshTokenService.cs:52-61`). `ClockSkew` varsayılan 5 dk (süre fiilen 35 dk). Anahtar ortam değişkeni yerine prod dosyasında (SEC-08). Rol değişikliği/pasifleştirme sonrası eski **access token ≤ 35 dk**, eski **refresh token** yenilemede reddedilir (AUTH-05 hariç — veli/servis için reddedilmez).

**AUTH önce/sonra:**

AUTH-01 (`KullaniciService.cs:72, 88`; `HesaplarController.cs:120`):
```csharp
// ÖNCE
_db.Kullanicilar.Add(model);                 // model.Rol formdan, GenelAdmin dahil
kullanici.Rol = model.Rol;
new Claim(ClaimTypes.Role, user.Rol.ToString()),
// SONRA
private static void RolDogrula(KullaniciRolu rol)
{
    if (rol is not (KullaniciRolu.Admin or KullaniciRolu.Ogretmen or KullaniciRolu.Servis or KullaniciRolu.Veli))
        throw new InvalidOperationException("Geçersiz rol.");
}
// EkleAsync: RolDogrula(model.Rol);  GuncelleAsync: if (kullanici.Rol == KullaniciRolu.GenelAdmin) throw ...; RolDogrula(model.Rol);
// HesaplarController (DB kullanıcısı):
if (user.Rol == KullaniciRolu.GenelAdmin) { ModelState.AddModelError("", "Geçersiz kullanıcı adı veya şifre."); return View(model); }
// config GenelAdmin'ine ek claim: new Claim("genelAdminKaynak", "config");
// Middleware + OkulSecim: context.User.IsInRole("GenelAdmin") && context.User.HasClaim("genelAdminKaynak", "config")
```

AUTH-02 (`OgrenciVeliFormVm.cs:8`, `OgrenciService.cs:113`):
```csharp
// ÖNCE
public OgrenciModel Ogrenci { get; set; } = new();
...
_db.Ogrenciler.Add(model);                   // bind edilmiş Veli/Ogretmen/koleksiyonlar da eklenir
// SONRA
public sealed class OgrenciFormDto
{
    public int OgrenciId { get; set; }
    [Required, StringLength(50)] public string OgrenciAdSoyad { get; set; } = "";
    [Range(1, int.MaxValue)] public int OgrenciNo { get; set; }
    [StringLength(10)] public string? OgrenciKartNo { get; set; }
    public int? VeliId { get; set; } public int? BirimId { get; set; } public int? ServisId { get; set; }
    public OglenCikisDurumu OgrenciCikisDurumu { get; set; }
    public bool IsDeleted { get; set; }
    public IFormFile? OgrenciGorselFile { get; set; }
}
var ent = new OgrenciModel {
    OgrenciAdSoyad = dto.OgrenciAdSoyad.ToUpper(_tr), OgrenciNo = dto.OgrenciNo,
    OgrenciKartNo = NormalizeKartNo(dto.OgrenciKartNo), VeliId = dto.VeliId,
    BirimId = dto.BirimId, ServisId = dto.ServisId, OgrenciCikisDurumu = dto.OgrenciCikisDurumu };
_db.Ogrenciler.Add(ent);
```

AUTH-03 (`OgrencilerController.cs:128-130`; aynısı Yemekhane/Ziyaretciler/Cihazlar/Kitaplar yazma action'ları):
```csharp
// ÖNCE
[HttpPost][ValidateAntiForgeryToken]
public async Task<IActionResult> Ekle(OgrenciVeliFormVm model, ...)
// SONRA
[MenuIslemYetki("Ogrenciler", "Index")]
[HttpPost][ValidateAntiForgeryToken]
public async Task<IActionResult> Ekle(OgrenciFormDto model, ...)
```

AUTH-04 (`Api/Program.cs:103-112`, API login/refresh action'ları, MVC `Program.cs`):
```csharp
// ÖNCE — tüm istemciler için tek sayaç, login'de limit yok
options.AddFixedWindowLimiter("arama", opt => { opt.PermitLimit = 10; opt.Window = TimeSpan.FromMinutes(1); opt.QueueLimit = 0; });
// SONRA
static string Ip(HttpContext c) => c.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor";
options.AddPolicy("arama", c => RateLimitPartition.GetFixedWindowLimiter(Ip(c),
    _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
options.AddPolicy("giris", c => RateLimitPartition.GetFixedWindowLimiter(Ip(c),
    _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
// [AllowAnonymous, EnableRateLimiting("giris")] → API login + refresh, MVC Hesaplar/Giris POST
// + Kullanicilar'a BasarisizGirisSayisi / KilitBitis; 5 hatada 15 dk kilit; StringLength(MinimumLength = 8)
```

AUTH-05 (`Api/Services/GirisService.cs:42-45`, aynısı :112-115):
```sql
-- ÖNCE
WHERE K.KullaniciAdi = @kullaniciAdi AND K.IsDeleted = 0
  AND (K.Rol <> 2 OR ISNULL(OP.IsDeleted, 0) = 0)
-- SONRA
WHERE K.KullaniciAdi = @kullaniciAdi AND K.IsDeleted = 0 AND K.Rol IS NOT NULL
  AND (K.Rol <> 2 OR ISNULL(OP.IsDeleted, 1) = 0)
  AND (K.Rol <> 3 OR ISNULL(SP.IsDeleted, 1) = 0)
  AND (K.Rol <> 4 OR ISNULL(V.IsDeleted, 1) = 0)
```
(Rol değerleri `Shared/Enums/KullaniciRolu.cs:8-18` ile uyumlu: Admin=1, Ogretmen=2, Servis=3, Veli=4, GenelAdmin=5.)

AUTH-06 (`Api/Services/RefreshTokenService.cs:29-35`):
```csharp
// ÖNCE
var eskiTokenlar = _tokenlar.Where(kvp => kvp.Value.KullaniciId == kullaniciId).Select(kvp => kvp.Key).ToList();
// SONRA (geçici; kalıcı çözüm DB tablosu: RefreshTokenlar(TokenHash, KullaniciId, CihazId, SonKullanim, IptalTarihi))
var simdi = _timeProvider.GetUtcNow().UtcDateTime;
var eskiTokenlar = _tokenlar
    .Where(kvp => (kvp.Value.KullaniciId == kullaniciId
                   && string.Equals(kvp.Value.OkulKodu, okulKodu, StringComparison.OrdinalIgnoreCase)
                   && kvp.Value.CihazId == cihazId)
               || kvp.Value.SonKullanim < simdi)
    .Select(kvp => kvp.Key).ToList();
// KullaniciTokenlariniSil(int kullaniciId, string okulKodu)
```

### 3.6 Rol bazlı saldırı senaryoları

| Rol | Senaryo | Sonuç | Kanıt |
|---|---|---|---|
| Veli | `GET api/ogrenciler/{baskaId}` / `/details` / `/haftalik-yoklama` ile başka öğrenci | **Engelliyor** | `Api/Controllers/OgrencilerController.cs:154, 176, 273, 391-419` |
| Veli | `GET api/gecis-kayit/{baskaId}` veya listede `sinifId` ile başka öğrenci | **Engelliyor** | `Api/Controllers/GecisKayitController.cs:39-44, 80-85` |
| Veli | Öğrenci no girerek kendini keyfi öğrenciyle eşleştirme | **Engelliyor** — eşleştirme yalnızca Admin'in `Ogrenciler.VeliId` set etmesiyle | `Api/Controllers/OgrencilerController.cs:193-238` (AdminOnly) |
| Veli | Yoklama/kayıt verisini değiştirme | **Engelliyor** — veli rolüne açık yazma ucu yok | 3.2 matrisi |
| Veli | Kendine rol atama | **Engelliyor** (API'de profil/rol ucu yok) | `KimlikDogrulamaController.cs:236-242` |
| Veli | `GET api/duyurular/{id}` ile başka sınıfın duyurusu | **İzin veriyor** | `Api/Controllers/DuyurularController.cs:48-54` (SEC-10) |
| Veli (MVC'de pasife alınmış) | Mobilden giriş yapıp çocuğun geçiş/yoklamasını görme | **İzin veriyor** | `Api/Services/GirisService.cs:42-45` (AUTH-05) |
| Veli | Panele mobil şifresiyle girip `/kartOkuHub`'ı dinleme | **İzin veriyor** (okuldaki tüm geçişler canlı; çok okulda tüm okullar) | `Controllers/HesaplarController.cs:104-131`; `Hubs/KartOkuHub.cs:6-9` |
| Öğretmen | Başka sınıfın öğrenci listesi / yoklaması okuma | İzin veriyor (iş kuralı **doğrulanmadı**) | `Api/Controllers/OgrencilerController.cs:84-130` |
| Öğretmen | Başka sınıfa yoklama yazıp velilere devamsızlık SMS'i | **İzin veriyor** | `Api/Controllers/OgrencilerController.cs:314-335`; `Api/Services/OgrenciService.cs:327-328` |
| Öğretmen | Herhangi öğrencinin veli adresi/telefonu/e-postası/kart no | **İzin veriyor** | `Api/Services/OgrenciService.cs:420-471` (SEC-02) |
| Öğretmen (panelde yalnızca "Öğrenci Veli Raporu" menüsü) | `POST /Ogrenciler/Ekle`'ye `Ogrenci.Veli.*` alanları ekleyip yeni Admin hesabı açma | **İzin veriyor** | AUTH-02 + AUTH-03 |
| Öğretmen (panelde Kitaplar menüsü) | Kitap görseli olarak `x.html` yükleyip Admin'e açtırma; `KitapGorsel=../web.config` ile dosya sildirme | **İzin veriyor** | SEC-04, SEC-05 |
| Servis | `GET api/servisler/{baskaServisId}/ogrenciler` | **Engelliyor** | `Api/Controllers/ServisController.cs:189-200` |
| Servis | `POST api/servisler/yoklama-kaydet` içinde başka servisin öğrencisi → veliye sahte SMS | **İzin veriyor** | `Api/Services/ServisService.cs:195` (SEC-01) |
| Servis | Görev için gereken minimum bilgi mi? | **Hayır** — kendi servisindeki öğrencinin veli adresi, e-posta, meslek, iş yeri, kart no | SEC-02 |
| Servis | Konum güncellemesini başka araç adına gönderme | **Uygulanamaz** — konum özelliği yok | — |
| Admin (okul) | Kullanıcıya/kendine GenelAdmin rolü verip başka okulu seçme | **İzin veriyor** | AUTH-01 |
| Admin (okul) | Session'a doğrudan başka okul kodu yazma | **Engelliyor** (session sunucu tarafında, yalnızca GenelAdmin için okunuyor) | `MvcTenantCozumleyiciMiddleware.cs:25-33` |
| Admin (API) | JWT'deki `okulKodu` dışında başka okul verisi | **Engelliyor** (imzalı claim) | `Api/Middleware/TenantCozumleyiciMiddleware.cs:23-40` |
| Silinen/rolü düşürülen panel kullanıcısı | Mevcut cookie ile devam | **İzin veriyor** (sliding) | AUTH-08 |
| Anonim | Login brute force (4 karakter şifre), kullanıcı adı enumerate | **İzin veriyor** | AUTH-04 |
| Anonim | Öğrenci fotoğrafını URL'den indirme | İzin veriyor (yeni adlar GUID; eski adlar **doğrulanmadı**) | SEC-09 |
| Çok rollü kullanıcı | Hem öğretmen hem veli | **Uygulanamaz** — tek rol alanı; iki hesap gerekir | `KimlikDogrulamaController.cs:279-296` |

---

## 4. AŞAMA 4 — Gerçek Zamanlı ve Arka Plan

### 4.1 Eşzamanlılık (CONC)

Hiçbir entity'de `RowVersion`/`[Timestamp]`/`ConcurrencyCheck` yok. Tekillik: `OgrenciKartNo` (NULL hariç) ve `KullaniciAdi` unique; **`OgrenciNo` unique değil** (yorum aksini söylüyor).

| ID | Öncelik | Kategori | Dosya:Satır | Sorun | Çözüm |
|---|---|---|---|---|---|
| CONC-01 | Orta | Check-then-insert | MVC `Services/Implementations/GecisService.cs:45, 92-111, 128-131`; `Models/OgrenciDetayModel.cs:7-9` | Geçiş yönü "son kaydı oku → ekle" ile ReadCommitted'da kilitsiz belirleniyor; çift okutma (veya WRK-03'teki çift event) aynı anda gelirse **iki GİRİŞ**, arka arkaya gelirse aynı saniyede giriş+çıkış; debounce yok | `sp_getapplock('GECIS:{ogrId}:{gun}')` (Yemekhane'deki desen) + N sn debounce |
| CONC-02 | Orta | Tutarsız tekillik | `GecisService.cs:49-81` ↔ `Services/BackgroundServices/YemekhanePollingService.cs:267-311` | Yemekhane tekilliği bir yolda applock, diğerinde kilitsiz | Aynı applock / filtreli unique index |
| CONC-03 | Orta | Çift SMS | MVC `SmsGonderimService.cs:52, 71-88`; `BekleyenSmsRetryService.cs:64-69, 119-164`; API `Services/BackgroundServices/BekleyenYoklamaSmsRetryService.cs:89-98, 151-167, 200-209` | Fire-and-forget gönderim ile 2 dk'lık retry aynı satırı yarışarak işler (SMS HTTP ≤30 sn, bayrak sonradan set); SMS `SaveChanges`'tan önce gönderiliyor; API retry'ında atomik claim yok. (MVC ve API retry'ları **farklı tabloları** işler — backend'ler arası yarış yok) | `UPDATE … SET Durum='Gonderiliyor' OUTPUT … WHERE Durum='Bekliyor'` ile claim → gönder; tek gönderici outbox |
| CONC-04 | Orta | Yoklama çift satır | API `Services/OgrenciService.cs:324-338`; `ServisService.cs:192-206`; `Data/AppDbContext.cs:311, 334` | `MERGE` `HOLDLOCK` olmadan, günlük unique kısıt yok → çift tıklama / iki öğretmen aynı anda kaydedince aynı gün çift satır | `MERGE … WITH (HOLDLOCK)` + `(OgrenciId, Tarih, Ders/Periyot)` unique |
| CONC-05 | Orta | Randevu çakışması | API `Services/RandevuService.cs:131-153, 295-318`; `AppDbContext.cs:398-401`; `OgretmenRandevuService.cs:50-79` | Çakışma kontrolü ve INSERT ayrı bağlantılarda, transaction yok → eşzamanlı çift randevu; öğretmen slotunda unique ihlali 500 döner | Tek transaction + `UPDLOCK, HOLDLOCK`; SqlException 2601/2627 → 409 |
| CONC-06 | Orta | Benzersizlik | `Data/AppDbContext.cs:345-347`; `OgrenciService.cs:97-122, 157` | `OgrenciNo` unique değil → aynı numaralı iki öğrenci; kart no çakışmasında `DbUpdateException` controller'da mesajsız yutuluyor | Unique index + alan bazlı hata |
| CONC-07 | Düşük | Çeşitli | Tüm modeller (RowVersion yok → aidat/tarife/kullanıcıda son yazan kazanır); `BirimlerController.cs:52-58` (BirimAd unique değil); `KullaniciService.cs:154-158, 187-190`, `Helpers/OgretmenAtamaHelper.cs:23-30` (SaveChanges sonrası transaction dışı `ExecuteUpdate`); `BekleyenSmsRetryService.cs:81-85` (MAX_DENEME başarılı SMS'leri de sayıyor) | Kritik tablolara `[Timestamp]`, transaction |

**Check-then-insert örneği** (CONC-01, `GecisService.cs:92-131` özet):
```csharp
// ÖNCE
var son = await _db.OgrenciDetaylar.Where(d => d.OgrenciId == ogrId && ...).OrderByDescending(...).FirstOrDefaultAsync();
var tip = son?.OgrenciCTarih == null && son?.OgrenciGTarih != null ? "Çıkış" : "Giriş";   // iki istek aynı "son"u görür
_db.OgrenciDetaylar.Add(new OgrenciDetayModel { ... });
// SONRA
await _db.Database.ExecuteSqlInterpolatedAsync(
    $"EXEC sp_getapplock @Resource={$"GECIS:{ogrId}:{zaman:yyyyMMdd}"}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000");
// (aynı transaction içinde) son kaydı oku → son kayıt < 10 sn ise yok say (debounce) → ekle → commit
```

### 4.2 SignalR (SR)

Kullanım: yalnızca MVC paneli, `KartOkuHub` (`/kartOkuHub`) — kart okuma kiosk ekranına geçiş bilgisi yayını. Mobil SignalR kullanmıyor (`HubConnection` yok); mobil gerçek zamanlılık FCM + DB'deki `Bildirimler` tablosu (kaçırılan push liste/okunmamış sayısından geri kazanılır, `Mobil/Services/BildirimService.cs:8, 22`). Kapı okutmaları mobile push değil **SMS** olarak gider.

| ID | Öncelik | Kategori | Dosya:Satır | Sorun | Çözüm |
|---|---|---|---|---|---|
| SR-01 | **Yüksek** | Veri sızıntısı (KVKK) | `Services/BackgroundServices/KartOkumaOlayIsleyiciService.cs:150`; `Controllers/KartOkuController.cs:46`; `Program.cs:193` | Tüm yayınlar `Clients.All`: grup yok. Geçiş verisi (ad soyad, no, sınıf, foto URL, saat) **giriş yapmış her kullanıcıya** (Veli/Servis dahil — AUTH-07) ve çok okulda **tüm okullara** gider. İstemci GUID filtresi (`wwwroot/js/signalr-handler.js:415-432`) yalnızca gösterimi engeller; veri DevTools'ta görünür. Anonim bağlantı **yok** (FallbackPolicy hub'a da uygulanıyor) ama `MenuYetkiFilter`/`TenantZorunluFilter` MVC filtresi olduğundan hub'a uygulanmaz | Hub'a rol/policy; `OnConnectedAsync`'te grubu **sunucu** belirlesin (`okul:{okulKodu}:cihaz:{guid}`), cihaz GUID'i tenant DB'sinde doğrulansın; `Clients.Group(...)` |
| SR-02 | **Yüksek** | Taklit | `Hubs/KartOkuHub.cs:8-9` | İstemciden çağrılabilen `KartOkunduBildir(dto)` istemci DTO'sunu `Clients.All`'a yayıyor → oturumu olan herkes tüm kiosklara sahte "Geçiş başarılı"/sahte öğrenci basabilir. JS bu metodu hiç kullanmıyor | Metodu sil |
| SR-03 | Orta | Dayanıklılık | `wwwroot/js/signalr-handler.js:68, 518-551` | `withAutomaticReconnect([0,2,5,10,30])` tükendikten sonra `onclose` yeniden başlatmıyor → kiosk sayfa yenilenene kadar ölü | `onclose`'da backoff ile `start()` veya sonsuz retry policy |
| SR-04 | Düşük | İstemci filtresi | `signalr-handler.js:415-432` | `CIHAZ_GUID` boşsa tüm mesajlar gösteriliyor | SR-01 ile gereksizleşir |

- **Grup adı:** grup kullanılmıyor; istemci grup belirleyemiyor ama zaten herkes her şeyi alıyor.
- **Query string JWT:** SignalR MVC'de **cookie** ile doğrulanıyor; `access_token` query string kullanımı yok → loglara token düşme riski yok.
- **Bağlantı–kullanıcı eşlemesi:** statik sözlük / `OnDisconnectedAsync` yok; temizlenecek durum yok.
- **Scale-out:** **Gerekli değil.** Tek IIS sunucusu, InProcess, x86, tek süreç. Kod tek süreç varsayıyor: Singleton ZKTeco COM bağlantısı (`Program.cs:91`), süreç içi session (`Program.cs:73`), in-memory refresh token'lar, backplane yok. Web garden (`maxProcesses>1`) veya overlapped recycle'da: aynı cihaza iki bağlantı, çift geçiş kaydı (CONC-01), çift SMS, POST'u işleyen süreç ile kiosk bağlantısının süreci farklıysa yayının kiosk'a ulaşmaması, GenelAdmin session kaybı. Doğru yapı: `maxProcesses=1` + OPS-01. İleride yatay ölçek gerekirse **önce** ZKTeco/arka plan işlerini ayrı Windows Service'e ayır (ARCH-01), ardından web katmanı için Redis backplane (on-prem IIS'te Azure SignalR Service anlamlı değil; yalnızca Azure'a taşınırsa) + dağıtık session + DB'de refresh token.

**SR-01/SR-02 önce/sonra** (`Hubs/KartOkuHub.cs`, `KartOkumaOlayIsleyiciService.cs:150`):
```csharp
// ÖNCE
public class KartOkuHub : Hub
{
    public async Task KartOkunduBildir(OgrenciBilgisiDto dto, CancellationToken ct)
        => await Clients.All.SendAsync("OgrenciBilgisiAl", dto, ct);
}
await hub.Clients.All.SendAsync("OgrenciBilgisiAl", dto, cts.Token);

// SONRA
[Authorize(Roles = "Admin,GenelAdmin,Ogretmen")]      // kiosk hesabının rolüne göre (doğrulanmadı)
public class KartOkuHub : Hub
{
    public static string Grup(string okulKodu) => $"okul:{okulKodu}";
    public override async Task OnConnectedAsync()
    {
        var tenant = Context.GetHttpContext()!.RequestServices.GetRequiredService<TenantBaglami>();
        if (string.IsNullOrEmpty(tenant.OkulKodu)) { Context.Abort(); return; }
        await Groups.AddToGroupAsync(Context.ConnectionId, Grup(tenant.OkulKodu));
    }
    // İstemciden çağrılabilir metot YOK
}
await hub.Clients.Group(KartOkuHub.Grup(okulKodu)).SendAsync("OgrenciBilgisiAl", dto, cts.Token);
// Program.cs: app.MapHub<KartOkuHub>("/kartOkuHub").RequireAuthorization("AdminOnly");  (veya kiosk policy)
```

### 4.3 ZKTeco arka plan işleri (WRK)

**Veri akışı (Ana Kapı, gerçek zamanlı):** `ZKTecoService.ConnectAsync` (Singleton, `ZKTecoService.cs:40-102`; yeni scope → tenant boş → **ilk okulun** `Cihazlar` tablosundan **ilk** ZKTeco cihazı :46-56) → `new CZKEM()` thread-pool'da (:66) → `Connect_Net` (:68) → `RegEvent` + `OnAttTransactionEx`/`OnHIDNum` (:75-82) → event'te `Task.Run` fire-and-forget (:185-238; cihaz zaman damgası kullanılmıyor) → `KartOkumaOlayIsleyiciService.KartOkumaIsleyiciAsync` (`now = DateTime.Now` :47; yeni scope → ilk okul :52; `OgrenciKartNo` ile ara :57-58; cihazı **yeniden** "ilk ZKTeco" diye seç :67-70) → `GecisService.KaydetAsync` (:79) → SMS `Task.Run` (:87-99) → SignalR `Clients.All` (:133, 150). Hata → yalnızca log (:135-138).
**Yemekhane:** `YemekhanePollingService` 1 dk döngü → ilk okul DB (:67) → cihazları sırayla `Connect_Net` + `ReadGeneralLogData` (:110-122, 169-200) → yalnızca **bugünün** logları (:210) → `sp_getapplock` ile tekil insert (:267-311) → bugün logu olmayan cihazda `ClearGLog` (:120-121, 148-149).

| ID | Öncelik | Kategori | Dosya:Satır | Sorun | Çözüm |
|---|---|---|---|---|---|
| WRK-01 | **Yüksek** (çok okulda) | Multi-tenant | `Program.cs:45-57`; `ZKTecoService.cs:46-47`; `KartOkumaOlayIsleyiciService.cs:52-53`; `YemekhanePollingService.cs:67-68, 353-354`; `BekleyenSmsRetryService.cs:53-54`; `RandevuArkaPlanService.cs:44-45, 67-68`; `BildirimCihaziTemizlemeService.cs:50-51` | MVC'deki 6 arka plan işinin **hiçbiri okulları dolaşmıyor**; hepsi ilk okulun DB'siyle çalışıyor. İkinci okul eklendiğinde o okulda ZKTeco, yemekhane, SMS retry, randevu hatırlatma ve token temizliği **hiç çalışmaz**. (API'deki `BekleyenYoklamaSmsRetryService` doğru dolaşıyor: `:53-67`) | Okul başına scope yardımcısı (aşağıda), fallback'i kaldır |
| WRK-02 | **Yüksek** | Kopma tespiti / veri kaybı | `ZKTecoService.cs:25, 44, 88, 142-180`; `DisconnectAsync` (:104) hiçbir yerden çağrılmıyor | `IsConnected` yalnızca bağlanınca `true` yapılıyor; `OnDisConnected` aboneliği, heartbeat veya hata tespiti yok. Cihaz ağdan düşerse izleyici `IsConnected==true` gördüğü için **hiç yeniden bağlanmaz**, okutmalar sessizce kaybolur. Mevcut exponential backoff (:175-176, 3 sn→60 sn, üst sınırlı) bu yüzden fiilen devreye girmez | Periyodik `GetDeviceTime` canlılık kontrolü + `OnDisConnected`; başarısızsa bağlantıyı sıfırla |
| WRK-03 | Orta | Eşleştirme kırılganlığı | `CihazService.cs:103-117, 248-255` (cihaza `EnrollNumber = OgrenciId` yazılıyor); `ZKTecoService.cs:185-217` (`AttTransactionEx`: `EnrollNumber` "kartNo" olarak iletiliyor), `:220-238` (`HIDNum`: gerçek kart no); `KartOkumaOlayIsleyiciService.cs:57-62` (`OgrenciKartNo == norm` ile aranıyor); `Models/OgrenciModel.cs:24` (kart no ≤10 karakter) | Sistem sahada çalışıyor (kullanıcı teyidi): gerçek zamanlı eşleşme fiilen **`OnHIDNum`** üzerinden (gerçek kart no → `OgrenciKartNo`) yürüyor. `OnAttTransactionEx` ise EnrollNumber'ı (=OgrenciId) kart no gibi arıyor → normalde bulunamıyor, yalnızca "ZKTeco kart tanımsız" log gürültüsü. Kalan riskler: (a) bir öğrencinin normalize edilmiş kart no'su başka bir öğrencinin Id'sine eşitse (ör. kısa kart no) yanlış öğrenci kaydedilir, velisine SMS gider ve aynı okutma iki kez işlenir (CONC-01); (b) sistem tek bir olaya bağımlı — `HIDNum` göndermeyen cihaz modeli/firmware'de gerçek zamanlı yol tamamen durur. Hangi olayların geldiği sunucu stdout logundan doğrulanabilir (`ZK HIDNum` + `ZK AttTransaction` + "ZKTeco kart tanımsız" satırları) | `AttTransactionEx`'te `OgrenciId = int.Parse(EnrollNumber)` ile eşleştir; `HIDNum`'u kaldır veya ayrı ele al; iki event'i tekilleştir |
| WRK-04 | **Yüksek** | Veri kaybı (outbox yok) | `KartOkumaOlayIsleyiciService.cs:135-138`; `ZKTecoService.cs:210-214` | DB kapalı / exception → okutma yalnızca loglanır; kuyruk/buffer/outbox yok; Ana Kapı cihazlarından `ReadGeneralLogData` ile **uzlaştırma yok** (loglar cihazda durur ama okunmaz). `EnableRetryOnFailure` (`Program.cs:40`) yalnızca kısa kesintiyi kapatır | Periyodik uzlaştırma: `(CihazId, OgrenciId, OkutmaZamani)` unique + idempotent insert + kalıcı imleç |
| WRK-05 | **Yüksek** | Veri kaybı (cihaz logu silme) | `YemekhanePollingService.cs:120-121, 148-149, 182-187, 209-210, 317-333` | Yalnızca bugünün logları işleniyor; servis/DB bir gün kapalı kalırsa o gün hiç aktarılmaz, ertesi gün ilk okutmadan önce `cihazOk && !bugunLogVar` → `ClearGLog` **tüm geçmişi siler**. Okuma ile silme arasında yeni bağlantı açılıyor → aradaki okutma da silinir | Otomatik `ClearGLog`'u kaldır; cihaz başına kalıcı "son işlenen zaman" imleci |
| WRK-06 | Orta | COM/STA | `ZKTecoService.cs:66, 75-82` | `CZKEM` MTA thread-pool thread'inde, mesaj döngüsü yok; zkemkeeper event'leri genelde STA + mesaj pompası ister → event'ler gelmeyebilir/sporadik. **Doğrulanmadı** (sahada test) | Özel STA thread + mesaj döngüsü, tüm `CZKEM` çağrıları o thread'de |
| WRK-07 | Orta | Cihaz seçimi | `ZKTecoService.cs:50-56`; `KartOkumaOlayIsleyiciService.cs:67-70` | Yalnızca **tek** cihaza bağlanılıyor, istasyon filtresi yok (ilk ZKTeco yemekhane cihazı olabilir); handler cihazı ayrıca yeniden seçiyor → bağlı cihazdan farklı `CihazId`/istasyon kaydedilebilir; `CurrentCihazId` kullanılmıyor | Cihaz başına bağlantı, `CihazId`'yi event'e closure ile taşı |
| WRK-08 | Orta | İş kuralı tutarsızlığı | `KartOkumaOlayIsleyiciService.cs:79` ↔ `KartOkuController.cs:131-245` | ZK yolu öğle çıkış izni ve yemek izni kontrollerini atlıyor, USB yolu yapıyor | Ortak servis |
| WRK-09 | Orta | Saat farkı | `KartOkumaOlayIsleyiciService.cs:47`; `ZKTecoService.cs:190-195`; `YemekhanePollingService.cs:204, 209` | Gerçek zamanlı yolda sunucu saati, polling'de cihaz saati (saniyesiz); `SetDeviceTime`/fark kontrolü yok; gece yarısı/saat kaymasında "bugün" filtresi kayıt atlar | Tek kaynak (cihaz saati) + periyodik senkron + fark uyarısı |
| WRK-10 | Orta | Bloklama | `KartOkumaOlayIsleyiciService.cs:30`; `YemekhanePollingService.cs:110-122, 171` | Senkron `Connect_Net` timeout'suz; yanıt vermeyen cihaz host başlatmayı ve diğer cihazları **geciktirir** (cihaz başına try/catch olduğu için durdurmaz, :237-240) | `SetCommTimeOut`, bağlantıyı `StartAsync` dışına taşı |
| WRK-11 | Düşük | Yaşam döngüsü | `ZkBaglantiIzleyiciHostedService.cs:26-29`; `KartOkumaOlayIsleyiciService.cs:37-41`; `ZKTecoService.cs:126` | Beklenmeyen exception'da izleyici sessizce biter (host durmaz, izleme de dönmez); `StopAsync` COM'u release etmiyor; `DisconnectAsync` handler'ı null'lıyor → çağrılırsa abonelik geri gelmez | İzleyici döngüsünü dayanıklı yap, `StopAsync`'te release |

Yanıtlar: **Tek cihazın hatası tüm servisi durduruyor mu?** Hayır (cihaz başına try/catch), ama geciktiriyor (WRK-10); gerçek zamanlı yol zaten tek cihaz (WRK-07). **Yakalanmayan exception servisi öldürüyor mu?** Döngüler `Exception` yakalıyor, host çökmez; izleyici sessizce bitebilir (WRK-11). **Cihaz şifresi/IP nerede?** `Cihazlar` tablosunda düz metin IP/port, CommKey yok (SEC-15). **Biyometrik veri aktarılıyor mu?** Hayır (SEC-15).

**WRK önce/sonra:**

WRK-01 — okul başına scope (tüm MVC arka plan servisleri için desen, ör. `YemekhanePollingService.cs:65-68`):
```csharp
// ÖNCE
using var scope = _scopeFactory.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();   // TenantBaglami boş → ilk okul
// SONRA
public static class TenantScope
{
    public static IServiceScope Olustur(IServiceScopeFactory f, OkulBilgiAyari okul)
    {
        var scope = f.CreateScope();
        var t = scope.ServiceProvider.GetRequiredService<TenantBaglami>();
        t.OkulKodu = okul.OkulKodu; t.ConnectionString = okul.ConnectionString; t.OkulAdi = okul.OkulAdi;
        return scope;
    }
}
foreach (var okul in _okulServisi.TumOkullariGetir())
{
    try
    {
        using var scope = TenantScope.Olustur(_scopeFactory, okul);
        await YemekhaneLogIsle(scope.ServiceProvider, ct);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    { _logger.LogError(ex, "Yemekhane turu hatası. Okul:{Okul}", okul.OkulKodu); }
}
```
(`OkulYapilandirmaServisi.TumOkullariGetir()` mevcut: `OgrenciBilgiSistemi.Shared/Services/OkulYapilandirmaServisi.cs:24-25`. `ZKTecoService` bağlantıları `(OkulKodu, CihazId)` anahtarıyla tutmalı.)

WRK-02 (`ZKTecoService.cs:150-167`):
```csharp
// ÖNCE
if (!IsConnected) { var ok = await ConnectAsync(); ... } else { ardisikBasarisiz = 0; }
// SONRA
if (IsConnected && !CanliMi())
{
    _logger.LogWarning("ZKTeco yanıt vermiyor, bağlantı sıfırlanıyor.");
    await BaglantiyiSifirlaAsync();   // -= handler, Disconnect, FinalReleaseComObject, IsConnected=false (_internalCardHandler'a dokunmadan)
}
if (!IsConnected) { if (await ConnectAsync()) ardisikBasarisiz = 0; else ardisikBasarisiz++; }
else ardisikBasarisiz = 0;

private bool CanliMi()
{
    int y = 0, mo = 0, d = 0, h = 0, mi = 0, s = 0;
    try { return _zkemkeeper?.GetDeviceTime(MACHINE_NO, ref y, ref mo, ref d, ref h, ref mi, ref s) == true; }
    catch { return false; }
}
```

WRK-03 — Orta, isteğe bağlı sağlamlaştırma (`KartOkumaOlayIsleyiciService.cs:57-58`, `ZKTecoService.cs:198`):
```csharp
// ÖNCE — EnrollNumber (=OgrenciId) kart no gibi aranıyor
var ogr = await db.Ogrenciler.AsNoTracking().FirstOrDefaultAsync(o => o.OgrenciKartNo == norm && !o.IsDeleted);
// SONRA — event kimlik türünü ve cihaz zamanını taşır
// ZKTecoService: handler(new ZkOkutma(CihazId, OgrenciId: int.Parse(EnrollNumber),
//                                     CihazZamani: new DateTime(Year, Month, Day, Hour, Minute, Second)));
// OnHIDNum aboneliği kaldırılır (aynı okutmayı ikinci kez tetikliyor)
var ogr = await db.Ogrenciler.AsNoTracking().FirstOrDefaultAsync(o => o.OgrenciId == okutma.OgrenciId && !o.IsDeleted, ct);
```

WRK-04 (`KartOkumaOlayIsleyiciService.cs:135-138`):
```csharp
// ÖNCE
catch (Exception ex) { _logger.LogError(ex, "Kart okuma işleminde hata. Kart: {Kart}", norm); }   // okutma kaybolur
// SONRA
catch (Exception ex) { _logger.LogError(ex, "Okutma işlenemedi; uzlaştırmada tekrar çekilecek. CihazId:{C}", okutma.CihazId); }
// + AnaKapiUzlastirmaService (okul başına, ör. 5 dk): ReadGeneralLogData → SonIslenenZaman'dan sonrası →
//   INSERT … WHERE NOT EXISTS (unique (CihazId, OgrenciId, OkutmaZamani)) → SonIslenenZaman güncelle
```

WRK-05 (`YemekhanePollingService.cs:120-121, 148-149, 210`):
```csharp
// ÖNCE
if (cihazOk && !bugunLogVar) temizlenecekCihazlar.Add((cihaz.IpAdresi!, cihaz.PortNo!.Value));
foreach (var (ip, port) in temizlenecekCihazlar) CihazLogTemizle(ip, port);   // tüm geçmiş silinir
if (ts.Date != today) continue;                                              // dünün kayıtları atlanır
// SONRA
// Otomatik ClearGLog kaldırıldı; cihaz başına kalıcı imleç
var sonIslenen = await db.CihazLogImlecleri.Where(x => x.CihazId == cihaz.CihazId)
                         .Select(x => (DateTime?)x.SonZaman).FirstOrDefaultAsync(ct) ?? today;
if (ts <= sonIslenen) continue;   // geçmiş günler de işlenir; günlük yemek kuralı ts.Date ile uygulanır
// commit sonrası imleç = max(ts); temizlik yalnızca manuel/gece bakımı ve imleçten eski kayıtlar için
```

---

## 5. AŞAMA 5 — Operasyon

### 5.1 Loglama ve izleme (OPS)

Durum: Serilog / OpenTelemetry / Application Insights **yok**, `AddHealthChecks` **yok**, correlation ID middleware'i **yok** (kod genelinde arama: 0 sonuç). Varsayılan `Microsoft.Extensions.Logging` sağlayıcıları (Console, Debug, EventSource, Windows'ta EventLog — EventLog varsayılan olarak yalnızca Warning+). MVC `web.config:8` `stdoutLogEnabled="true"` → tek kalıcı log `.\logs\stdout_*.log` (rotasyon/boyut sınırı yok); API `web.config` `stdoutLogEnabled="false"` → API'nin Information logları **hiçbir kalıcı yere yazılmıyor**, hatalar yalnızca Windows Event Log'da (sunucuda ek log hedefi **doğrulanmadı**).

| ID | Öncelik | Kategori | Dosya:Satır | Sorun | Çözüm |
|---|---|---|---|---|---|
| OPS-01 | Orta | Hosted service sürekliliği | `OgrenciBilgiSistemi/web.config:8` (yalnızca `hostingModel`); `Program.cs:195-196` (`/keep-alive`) | Repoda IIS `startMode=AlwaysRunning`, preload, `idleTimeout=0` yok. Varsayılan 20 dk boşta kalınca app pool durur → **ZKTeco, yemekhane, SMS retry durur**; `/keep-alive` yalnızca kiosk sayfası açıkken çalışır. Sunucu ayarı **doğrulanmadı** | App pool: AlwaysRunning + preloadEnabled + idleTimeout=0 + `maxProcesses=1`; periyodik recycle'ı gece saatine |
| OPS-02 | Orta | Yapılandırılmış log / korelasyon | MVC/API `Program.cs` (logging yapılandırması yok); `web.config` | Yapılandırılmış, aranabilir log yok; API logları kalıcı değil; istek ile `Task.Run` SMS'i arasında korelasyon yok | Serilog (dosya, günlük rotasyon, 30 gün) + `Enrich.FromLogContext` + `TraceIdentifier`/`Activity.TraceId`'yi arka plan işine taşı; PII maskeleme (SEC-14) |
| OPS-03 | Orta | Health check | Yok | DB, ZKTeco bağlantısı, arka plan servislerinin son başarılı turu, SMS sağlayıcı izlenmiyor; WRK-02 gibi sessiz kopmalar fark edilmez | `AddHealthChecks().AddSqlServer(her okul).AddCheck<ZkBaglantiHealthCheck>()` + servislerin "son tur zamanı" check'i; `/health` (yetkili) + dış izleme |
| OPS-04 | Düşük | SMS sağlayıcı yanıtı | `OgrenciBilgiSistemi.Sms/SmsService.cs:80-87` | HTTP 200 dönen her yanıt başarılı sayılıyor; ilksms hata kodları gövdede ise gözden kaçar. **Doğrulanmadı** | Sağlayıcı yanıt formatını parse et |

**Somut senaryo — "Veli sabah 08:05 giriş SMS'i almadı":**
1. MVC stdout logunda `ZK AttTransaction: Kart=…` satırı aranır (kart UID'si düz metin — SEC-14). Satır **yoksa**: cihaz bağlantısı kopmuş olabilir (WRK-02) ama bunu gösteren log yok (izleyici `IsConnected=true` sanıyor), health check yok (OPS-03) → kök neden **bulunamaz**.
2. Satır **varsa**: `KaydetAsync` sonrası SMS `Task.Run` içinde; hata `SMS gönderim hatası. Öğrenci: {OgrId}` ile loglanır ama isteği/okutmayı bağlayan korelasyon ID'si yok. SMS yanlış tenant'a gittiyse (SEC-03) log bunu göstermez (okul kodu loglanmıyor).
3. `SmsGonderimGecmisleri` tablosunda aranır — hangi DB'de? Fallback nedeniyle **ilk okulun** DB'sinde olabilir.
4. App pool gece idle'a düştüyse (OPS-01) sabah ilk okutmalar hiç işlenmemiş olabilir; IIS'te bunu gösteren uygulama logu yok.
→ **Sonuç:** Bugünkü loglarla bu tür bir hatanın kaynağı güvenilir biçimde bulunamaz. OPS-01/02/03 + SEC-14 birlikte ele alınmalı.

### 5.2 Cache (CACHE)

**Mevcut kullanım:**

| Nerede | Ne | Anahtar | Süre | Invalidation |
|---|---|---|---|---|
| MVC `Services/Implementations/MenuService.cs:35-37, 178` | Kullanıcının menü ağacı | `menu:{okulKodu}:{kullaniciId}` | 5 dk | Yetki güncellemesinde `KullaniciService.cs:325` |
| MVC `Services/Implementations/AidatService.cs:38-54, 588` | Aidat tarife tutarı | `aidat_tarife:{okulKodu}:{yil}` | 30 dk | Tarife kaydında; silmede yok (Düşük) |
| MVC `Program.cs:73` | `AddDistributedMemoryCache` — yalnızca session deposu | – | 8 saat | – |
| API `RefreshTokenService` | Refresh token'lar (cache değil, durum) | – | 8 saat | → AUTH-06 |
| API | **Hiç cache yok** | | | |

Cache anahtarları doğru şekilde tenant'a göre ayrılmış (okullar arası cache sızıntısı yok).

**Çok okunan / az değişen veriler (aday):**
- `GET api/siniflar/all-with-count`, `GET api/ogretmenler/aktif`, `GET api/uygulama-versiyon`, `GET api/kimlik-dogrulama/okullar` — okul geneli, gün içinde nadiren değişir.
- MVC `MenuYetkiFilter`/`MenuIslemYetkiFilter` yetki sorguları (PERF-08) — her istekte.
- Veli başına `ogrenciler/benim` (çocuk listesi) — gün içinde değişmez; ama kişiye özel, küçük.
- Servis güzergâhı/öğrenci listesi (`servisler/{id}/ogrenciler`) — şoför başına, gün içinde nadiren değişir.
- **Cache'lenmemeli:** geçiş kayıtları, yoklama, bildirim sayıları (sabah tepe saatinde zaten değişen veri; çözüm PERF-01 indeks/sayfalama).

**Öneri:** Mevcut dağıtım **tek IIS sunucusu, tek süreç** → **`IMemoryCache` yeterli**. API'ye `AddMemoryCache` ekleyip yukarıdaki okul geneli uçlar için tenant anahtarlı 1–5 dk mutlak süreli cache; yetki filtresi için 1-5 dk + mevcut invalidation. .NET 10'daki `HybridCache` (L1 bellek + stampede koruması) sabah tepesinde aynı anahtara eşzamanlı isteklerin DB'ye yığılmasını önlediği için `IMemoryCache` yerine tercih edilebilir; L2 (Redis) **eklenmemeli**. **Redis gereksiz karmaşıklık**: tek sunucu, MVC ve API'nin paylaşması gereken cache durumu yok; yalnızca yatay ölçeğe geçilirse (bkz. 4.2) gündeme gelir.

---

## 6. Go / No-Go ve İlk 5 İş

### Karar

| Senaryo | Karar | Gerekçe |
|---|---|---|
| **Mevcut tek okullu kurulumun çalışmaya devam etmesi** | **Koşullu GO** — ilk 5 iş 1-2 hafta içinde | Okullar arası sızıntılar henüz aktif değil; ancak okul içi yetki yükseltme (AUTH-01/02), IDOR (SEC-01), KVKK aşırı veri (SEC-02), düz HTTP (SEC-06) ve rotasyonu yapılmamış sızmış kimlik bilgileri (SEC-07) **bugün** istismar edilebilir durumda |
| **İkinci okulun eklenmesi / yaygınlaştırma** | **NO-GO** | WRK-01, SEC-03, SR-01 düzeltilmeden ikinci okul eklenirse: yeni okulda kart okuma/SMS/randevu işleri hiç çalışmaz, SMS'ler yanlış okulun velisine gider, geçiş verileri tüm okullara yayınlanır |
| **Kart okuma verisinin resmi yoklama kaydı olarak kullanılması** | **NO-GO** (sahada doğrulanana kadar) | WRK-02 (kopma fark edilmiyor), WRK-03 (tek olaya bağımlılık), WRK-04/05 (outbox yok, cihaz logu siliniyor) |

### İlk yapılacak 5 iş

| # | İş | Bulgular | Tahmini efor |
|---|---|---|---|
| 1 | **Kimlik bilgisi rotasyonu ve secret'ları ortam değişkenine taşıma:** ilksms şifresi, GenelAdmin şifresi, (kullanılıyorsa) geçmişteki SQL şifresi; `Jwt__SecretKey`, connection string, SMS ve GenelAdmin değerleri IIS ortam değişkenine. Swagger'ı prod'da kapat, zafiyetli paketleri yükselt | SEC-07, SEC-08, SEC-09, DEP-01, DEP-02 | 0.5–1 gün |
| 2 | **Panelde yetki yükseltmeyi kapatma:** GenelAdmin rol atamasını reddet ve config kaynaklı işaretle; öğrenci formunu DTO'ya çevir (hemen: nav property'lere `[BindNever]`); yazma action'larına `MenuIslemYetki`; tüm yüklemeleri `LocalFileStorage`'a, silmeyi `uploads` ile sınırla | AUTH-01, AUTH-02, AUTH-03, SEC-04, SEC-05 | 2–3 gün |
| 3 | **API sahiplik ve KVKK düzeltmeleri + brute force koruması:** servis yoklamasını şoförün öğrencileriyle sınırla; rol bazlı öğrenci detay DTO'su (kart no'yu kaldır); pasif veli/servis girişini engelle; duyuru IDOR; IP bazlı rate limit (API + MVC login), kilitleme, min 8 karakter, `kullanici-ara`'yı kaldır; refresh token deposunu `(okulKodu, kullaniciId)` anahtarına al | SEC-01, SEC-02, AUTH-05, SEC-10, AUTH-04, AUTH-06 | 3–4 gün |
| 4 | **Mobil güvenlik sürümü:** HTTPS zorunlu (sunucuya sertifika + `KayitSunucuUrl`/`ApiUrl` https, cleartext/ATS kaldır), refresh deadlock düzeltmesi, "Beni Hatırla"da şifre saklamayı kaldır, `allowBackup=false` | SEC-06, BUG-01, SEC-17, SEC-18, SEC-16 | 2–3 gün + mağaza onayı |
| 5 | **Kart okuma güvenilirliği ve SignalR izolasyonu:** hub'daki istemci metodunu sil, hub'a policy + okul grubu; canlılık kontrolü; yemekhane `ClearGLog`'u kaldır; IIS AlwaysRunning; ardından (ikinci okuldan **önce**) arka plan servislerini okul döngüsüne al ve DbContext fallback'ini kaldır | SR-01, SR-02, WRK-02, WRK-05, OPS-01, WRK-01, SEC-03 | 4–6 gün |

Sonraki dalga: CONC-01/04 (geçiş ve yoklama tekilliği), WRK-03 (AttTransactionEx'i OgrenciId ile eşleştir + iki olayı tekilleştir; sahada tek cihazla doğrula), WRK-04 (uzlaştırma/outbox), OPS-02/03 (Serilog + health check), PERF-01 (geçiş sorgusu indeksleri), TEST-01 (yetki ve tenant izolasyonu entegrasyon testleri).
