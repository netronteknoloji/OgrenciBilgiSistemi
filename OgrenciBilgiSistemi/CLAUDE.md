# CLAUDE.md - OgrenciBilgiSistemi (MVC Katmanı)

.NET 10 MVC web paneli. Yöneticilerin kullandığı ana panel; tüm iş mantığı (EF Core), ZKTeco entegrasyonu ve SignalR kart okuma akışı burada. API ve Mobil ayrı projelerde — root `CLAUDE.md`'ye bak.

## Mimari

- Katmanlı monolith: Controllers → Services → Data.
- Multi-tenant: her okul ayrı SQL DB, connection string **per-request** → `DbContextPool` YASAK.
- Tenant: `MvcTenantCozumleyiciMiddleware` cookie/session'dan okul kodunu çözer.
- Auth: Cookie, login `/Hesaplar/Giris`, FallbackPolicy = authenticated.
- GenelAdmin: kimlik `appsettings.json` → `GenelAdmin` (hash'li) ile doğrulanır. Okul seçince o okul DB'sine `Rol=GenelAdmin` satırı yazılır; bu satırla oturum açılamaz, panelden bu rol atanamaz/düzenlenemez.
- Realtime: SignalR `KartOkuHub` → `/kartOkuHub`. Hub'da istemcinin çağırabileceği metot yok; yayın yalnızca sunucudan `IHubContext` ile. Bağlantı `OnConnectedAsync`'te okul grubuna (`KartOkuHub.OkulGrubu(okulKodu)`) girer, veli/servis girmez; yayınlar `Clients.Group(...)` ile — `Clients.All` YASAK.
- Dosya: `IFileStorage` → `LocalFileStorage` (magic bytes).
- **x86 zorunlu** (ZKTeco COM 32-bit).

## DI Ömürleri

- **Singleton:** `IZKTecoService`, `OkulYapilandirmaServisi`
- **Scoped:** `TenantBaglami`, `DbContext`, tüm `I*Service`
- İstek dışı DB erişimi (background service, `Task.Run`, hub) → `TenantKapsamFabrikasi.Olustur(okulKodu)` / `HerOkulIcinAsync(...)`. Çıplak `IServiceScopeFactory.CreateScope()` YASAK (tenant'sız kapsam).

## Global Filtreler (sırayla)

`AuthorizeFilter` → `MenuYetkiFilter` → `TenantZorunluFilter`

Çok menülü controller'larda action → menü eşlemesi `Infrastructure/MenuIslemHaritasi` (+ `MenuIslemYetkiAttribute`). Oturumdaki rol/menü bilgisi `OturumDogrulayici` ile ≤5 dk'da tazelenir.

## Paylaşılan Kurallar (root `CLAUDE.md`'de ayrıntı)

- **Kart okuma:** USB (`KartOkuController`) ve ZKTeco (`KartOkumaOlayIsleyiciService`) aynı kararı `IGecisKarariService` + `GecisKilidi` → `Shared/Kurallar/GecisKurallari` üzerinden verir. Kural burada değil, Shared'de ve testlerde değişir.
- **SMS + push = outbox:** üretici yalnızca `IGonderimKuyruguService` ile kuyruğa ekler, `KuyrukTetikleyici` işleyiciyi uyandırır; `ISmsService` / `IPushBildirimGonderici`'yi yalnızca `GonderimKuyruguIsleyici` çağırır.
- **Cihaz senkronu:** öğrenciyi cihaza `CihazService` ile doğrudan yazma; `ICihazSenkronService` ile kuyruğa ekle, `CihazSenkronIsleyici` yazar. ZKTeco cihazına erişen her kod `ZkCihazKilidi` altında çalışır ve bağlantıyı `IZKTecoService.CihazBaglantisiAcAsync` ile alır — kendi `Connect_Net`'ini açmaz. Cihaz ikinci SDK oturumunu reddeder (-307); olay bağlantısı o cihazdaysa aynı COM nesnesi ödünç verilir. Kilit sırası: `ZkCihazKilidi` → `ZKTecoService._connLock`.
- **Yemek hakkı:** tek tanım `YemekHakki.HakkiOlanlar(db, yil, ay)`; yemek hakkını başka yerde sorgu yazarak hesaplama.
- **Kart no:** kaydederken ve okuturken `Shared/Helpers/KartNo.Normalize`.

## Background Services (7 adet)

`KartOkumaOlayIsleyiciService`, `ZkBaglantiIzleyiciHostedService`, `YemekhanePollingService` (yalnızca cihaz logu), `GonderimKuyruguIsleyici` (SMS + push gönderimi, tüm okullar), `CihazSenkronIsleyici` (öğrenci ↔ ZKTeco cihaz senkronu; `CihazSenkronKuyrugu` outbox'ı), `RandevuArkaPlanService`, `BildirimCihaziTemizlemeService`.

## AppDbContext Tuzakları

- **`IncludeDeleted`** flag → çoğu entity'de global query filter. Pasif raporu için `true` yap.
- **Soft delete:** `RandevuModel`, `OgretmenRandevuModel`, `BildirimModel`, `DuyuruModel` → `Remove()` YASAK, `IsDeleted = true`.
- **Profil tabloları** (`VeliProfilModel`, `ServisProfilModel`, `OgretmenProfilModel`): PK = `KullaniciId`.
- **Menu seed:** 35 menü (Id 1-35); yeni eklerken Id 36'dan başla.
- **Check constraints:** `OgrenciAidatModel` (`BaslangicYil` 2000-2100, tutarlar ≥0), tarife ve ödeme tablolarında benzer.
- **Unique:** `OgrenciKartNo` (NULL hariç), `KullaniciAdi` (filtreli). `OgrenciNo` index'i unique **değil**.

## Yasaklar

- ❌ Controller'da `AppDbContext` → Service'ten geç.
- ❌ `wwwroot/uploads`'a doğrudan yazma → `IFileStorage`.
- ❌ ZKTeco çağrısını UI thread'de yapma → background service.
- ❌ `DbContextPool` / `AddDbContextPool`.
- ❌ Background service'de DbContext'i field olarak tutma.
- ❌ Hub içinde uzun işlem → service'e devret.
- ❌ Hassas veri (TC, şifre, kart UID) loglama.
- ❌ Yeni controller'da login akışı yaparken `[AllowAnonymous]` koymama.

## Komutlar

Repo kökünden (root `CLAUDE.md` ile aynı):

```bash
dotnet build OgrenciBilgiSistemi/OgrenciBilgiSistemi.csproj
dotnet run --project OgrenciBilgiSistemi/OgrenciBilgiSistemi.csproj
dotnet ef migrations add <Isim> --project OgrenciBilgiSistemi/OgrenciBilgiSistemi.csproj
dotnet ef database update --project OgrenciBilgiSistemi/OgrenciBilgiSistemi.csproj
dotnet ef migrations script --idempotent --project OgrenciBilgiSistemi/OgrenciBilgiSistemi.csproj
```

Elle SQL betiği (`Scripts/`) yazılırsa `__EFMigrationsHistory` kaydını da ekle.

## Klasörler

```
Controllers/   Views/   ViewModels/   Migrations/
Services/{Implementations,Interfaces,BackgroundServices}/
Data/AppDbContext.cs
Hubs/KartOkuHub.cs
Models/*Model.cs
Infrastructure/{FileStorage/LocalFileStorage, MenuYetkiAttribute (MenuYetkiFilter dahil), MenuIslemYetkiAttribute,
                MenuIslemHaritasi, OturumDogrulayici, TenantZorunluFilter, MvcTenantCozumleyiciMiddleware}.cs
Abstractions/   Lib/ (zkemkeeper interop)   wwwroot/uploads/ (git'e girmez)
```

## Endpoint'ler

- Default route: `Hesaplar/Giris`
- SignalR: `/kartOkuHub`
- Keep-alive: `/keep-alive` (auth)

## Naming

- Entity: `*Model` · Service: `I*Service`/`*Service` · Hub: `*Hub`
- DbSet/tablo: çoğul Türkçe (`Ogrenciler`)
- Kod dili: Türkçe

## Paketler

EF Core 10.0.9 (SqlServer + Tools), ClosedXML. **zkemkeeper** (COM, GUID `fe9ded34-e159-408e-8490-b720a5e632c7`): derleme `Lib/Interop.zkemkeeper.dll` (tlbimp çıktısı, `EmbedInteropTypes`) ile yapılır, COM kaydı gerekmez; çalışma anında sunucuda ZKTeco SDK'sı (SysWOW64, `regsvr32`) kayıtlı olmalı.
