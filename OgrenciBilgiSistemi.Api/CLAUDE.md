# CLAUDE.md - OgrenciBilgiSistemi.Api

Mobil için REST API. .NET 10, JWT, multi-tenant, **ham SQL** (EF Core YOK). Detaylı sistem bilgisi için root `CLAUDE.md`.

## Mimari

- ASP.NET Core Web API (Minimal hosting + Controllers).
- Auth: JWT Bearer (Issuer/Audience/Lifetime/SigningKey doğrulamalı).
- Tenant: JWT'deki `okulKodu` claim'i → `TenantCozumleyiciMiddleware` → `TenantBaglami`.
- DB: `Microsoft.Data.SqlClient` ile **ham ADO.NET**. EF YOK.
- Static files: MVC'nin `wwwroot`'unu serve eder. Yol: `MvcWwwRootPath` config, fallback `C:\inetpub\wwwroot\obs\wwwroot`.
- CORS: `Cors:AllowedOrigins[]`. **Boşsa hiçbir origin'e izin yok** (güvenli default).
- Rate limit: `arama` policy → IP başına 10 istek/dakika.
- Swagger: yalnızca Development'ta (canlıda geçici olarak `"Swagger:Etkin": true`). Canlıda HSTS açık.
- Hata: `UseExceptionHandler()` + ProblemDetails; yakalanmayan hata loglanır, istemciye ayrıntı gitmez. `catch` içinde `ex.Message` döndürme.
- Giriş: `GirisDenemeSayaci` (Shared) — aynı hesaba 15 dk'da 5 hatalı deneme → 15 dk kilit (429).
- **x86** (MVC ile tutarlılık; COM bağımlılığı yok).

## DI Ömürleri

- **Singleton:** `OkulYapilandirmaServisi`, `GirisDenemeSayaci`, `RefreshTokenService` (okul DB'sindeki `RefreshTokenlar`; token `{okulKodu}.{rastgele}`, DB'de SHA-256 hash; 8 saat, tek kullanımlık, çoklu cihaz; JWT'de `oturumId` claim'i → çıkış yalnızca o cihaz, şifre değişikliği tüm cihazlar)
- **Scoped:** `TenantBaglami`, tüm `*Service`
- **Hosted:** `YoklamaSmsKuyrukTarayici` (kuyruğa alınamamış yoklama SMS'lerini kuyruğa alır; gönderim MVC'de)

**Domain servisleri (hepsi Scoped, tam liste `Services/`):** `GirisService`, `SinifService`, `OgrenciService`, `GecisKayitService`, `ServisService`, `RandevuService`, `OgretmenRandevuService`, `BildirimService`, `OgretmenListeService`, `DuyuruService`, `YoneticiService`, `VeliListeService`, `YoklamaSmsBildirimService`. Push token deposu: `RawSqlBildirimTokenDeposu` (`IBildirimTokenDeposu`, Push projesinin sözleşmesi).

**Outbox:** SMS/push gönderimi API'de yapılmaz; `YoklamaSmsBildirimService` (yoklama SMS) ve `BildirimService` (push) yalnızca `GonderimKuyrugu`'na ekler, gönderim MVC'de. Tek istisna: `CihazlarController.TestPush` (AdminOnly tanı ucu) `IPushBildirimGonderici`'yi doğrudan çağırır.

## Authorization

- Global `FallbackPolicy` = kimlik doğrulanmış kullanıcı. Anonim uç için `[AllowAnonymous]` açıkça yazılmalı; rol kontrolü yine `[Authorize(Policy=...)]`/action içinde.
- `AdminOnly` policy → `RequireClaim("rol", "Admin", "GenelAdmin")` (MVC'de `RequireRole`, **burada `RequireClaim`**).

## Yasaklar

- ❌ EF Core / `AppDbContext` import → ham SQL kullan.
- ❌ Anonim olması gerekmeyen uca `[AllowAnonymous]` koyma.
- ❌ JWT secret'ı versiyona giren config'e yazma → env variable veya `appsettings.Development.json`.
- ❌ `Cors:AllowedOrigins = ["*"]` → `AllowCredentials` ile çakışır.
- ❌ Tenant context olmadan SQL → `TenantBaglami.ConnectionString` kontrol et.
- ❌ MVC `wwwroot` yolunu hardcode etme → `MvcWwwRootPath`.
- ❌ Domain modelini direkt döndürme → DTO kullan.
- ❌ Refresh token'ı düz metin saklama → yalnızca `RefreshTokenBicimi.Hash`.

## Endpoint Kalıpları

```csharp
[Authorize]                                      // standart
[Authorize(Policy = "AdminOnly")]                // admin
[AllowAnonymous, EnableRateLimiting("arama")]    // anonim arama
```

Response tipi: `IActionResult` veya `ActionResult<T>`.

## Konfigürasyon

```json
{
  "Okullar": [...],                  // MVC ile aynı
  "Jwt": { "SecretKey": "", "Issuer": "", "Audience": "" },
  "Cors": { "AllowedOrigins": ["https://..."] },
  "MvcWwwRootPath": "C:\\inetpub\\...",
  "SmsAyarlari": {...}               // MVC ile aynı
}
```

## Komutlar

```bash
dotnet run --project OgrenciBilgiSistemi.Api/OgrenciBilgiSistemi.Api.csproj   # repo kökünden
$env:Jwt__SecretKey = "..."          # PowerShell
# Swagger: https://localhost:<port>/swagger
```

## Klasörler

```
Controllers/   Services/{*, Interfaces/}   Middleware/   Dtos/   Models/   Program.cs
(YoklamaSmsKuyrukTarayici ve RefreshTokenService doğrudan Services/ altında)
```

## Paketler

`Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.8, `Microsoft.Data.SqlClient` 7.0.1, `Swashbuckle.AspNetCore` 10.3.0. ProjectRef: `Shared`, `Sms`, `Push`.
