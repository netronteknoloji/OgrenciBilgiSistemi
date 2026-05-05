# Proje Analizi Raporu — OgrenciBilgiSistemi

**Tarih:** 2026-04-09

---

## 1. HATALAR VE BUGLAR

### YÜKSEK

| # | Proje | Dosya | Satır | Sorun |
|---|-------|-------|-------|-------|
| 1 | ~~API~~ | ~~`OgrenciService.cs`~~ | ~~373-411~~ | ~~`UPDATE` sorgusu `WHERE OgrenciDurum=1` filtresi yok~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `WHERE` koşuluna `AND OgrenciDurum = 1` eklendi |
| 2 | ~~MVC~~ | ~~`AidatService.cs`~~ | ~~349-396~~ | ~~Ödeme ekleme transaction yok~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `ExecutionStrategy` + `BeginTransactionAsync` ile sarıldı |
| 3 | ~~MVC~~ | ~~`CihazService.cs` + `Program.cs`~~ | ~~18-19 / 94~~ | ~~Scoped kayıtlı~~ — ✅ **YANLIŞ TESPİT:** Program.cs satır 94'te zaten `AddSingleton` olarak kayıtlı |
| 4 | ~~Mobil~~ | ~~`GuncellemeKontrolService.cs`~~ | ~~16~~ | ~~URL çift `/api/` sorunu~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `$"{BaseUrl}uygulama-versiyon"` olarak düzeltildi |
| 5 | ~~API~~ | ~~`GirisService.cs`~~ | ~~33~~ | ~~`AdSoyad` mapping hatası~~ — ✅ **YANLIŞ TESPİT:** Kullanicilar tablosunda AdSoyad kolonu yok, KullaniciAdi mapping doğru |

### ORTA

| # | Proje | Dosya | Satır | Sorun |
|---|-------|-------|-------|-------|
| 6 | ~~MVC~~ | ~~`OgrenciService.cs`~~ | ~~113-137~~ | ~~Cihaz sync tutarsızlığı~~ — ✅ **DÜZELTİLDİ (2026-04-09):** Transaction commit sonrası `AsNoTracking` ile güncel entity okunarak cihaza gönderiliyor |
| 7 | ~~Mobil~~ | ~~`TemelApiService.cs`~~ | ~~168-175~~ | ~~Token refresh race condition~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `_aktifRefreshGorevi` (Task<bool>?) ile eşzamanlı thread'ler aynı görevi bekliyor |
| 8 | ~~Mobil~~ | ~~`App.xaml.cs`~~ | ~~13-19~~ | ~~Static event leak~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `-=` / `+=` pattern ile named handler, birikim önlendi |
| 9 | ~~MVC~~ | ~~`AppDbContext.cs`~~ | ~~349-353~~ | ~~Unique index yorum satırında~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `KullaniciAdi` unique index uncomment edildi |
| 10 | ~~Mobil~~ | ~~`KullaniciOturum.cs`~~ | ~~93-146~~ | ~~`_yuklendi` partial failure~~ — ✅ **DÜZELTİLDİ (2026-04-09):** catch bloğuna `_yuklendi = false` eklendi |
| 11 | ~~API~~ | ~~`BekleyenYoklamaSmsRetryService.cs`~~ | ~~155-156~~ | ~~SQL kolon adı interpolation~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `_dersKolonlari` static whitelist dictionary eklendi |

### DÜŞÜK

| # | Proje | Dosya | Sorun |
|---|-------|-------|-------|
| 12 | ~~Mobil~~ | ~~`OgrenciDetayView.xaml.cs:56-57`~~ | ~~Concurrent request~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `CancellationTokenSource` ile önceki istek iptal ediliyor |
| 13 | ~~Mobil~~ | ~~`KullaniciOturum.cs:98`~~ | ~~TTL bypass~~ — ✅ **DÜZELTİLDİ (2026-04-09):** Parse başarısızsa oturum geçersiz sayılıyor |
| 14 | ~~MVC~~ | ~~`CihazService.cs:492`~~ | ~~CardNumber hardcode~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `string.Empty` olarak değiştirildi |

---

## 2. GÜVENLİK AÇIKLARI

### YÜKSEK — Acil Müdahale

| # | Proje | Dosya | Sorun |
|---|-------|-------|-------|
| 15 | **API** | `appsettings.Production.json:10` | **JWT Secret Key** source control'da → herkes token forge edebilir, tam authentication bypass |
| 16 | **API** | `appsettings.json:6` | DB şifresi (`Netron.2016`) plaintext olarak committed |
| 17 | **MVC** | `appsettings.json:22-25` | SMS API credentials plaintext committed |
| 18 | **Mobil** | `Constants.cs:7,10` | Tüm API iletişimi **HTTP** (HTTPS değil) — kullanıcı adı, şifre, JWT token'lar açık metin olarak ağda |
| 19 | **Mobil** | `GirisView.xaml.cs:158-163` | "Beni Hatırla" özelliği **şifreyi cleartext** olarak SecureStorage'a kaydediyor |
| 20 | **MVC** | `KullaniciService.cs:293-307` | Profil resmi yükleme: magic byte kontrolü yok, extension whitelist yok — zararlı dosya yüklenebilir |
| 21 | **MVC** | `KitapService.cs:148-185` | Kitap kapağı yükleme: aynı sorun — dosya tipi doğrulaması yok |
| 22 | **API** | `Program.cs:120-121` | Swagger **production'da açık** — `IsDevelopment()` guard'ı yok, tüm API şeması herkese açık |

### ORTA

| # | Proje | Dosya | Sorun |
|---|-------|-------|-------|
| 23 | API | `KimlikDogrulamaController.cs:57-68` | `hash-uret` endpoint: şifre **URL query string'inde** taşınıyor — log'lara yazılır |
| 24 | API | `GirisService.cs:141-163` | Şifre değiştirme **eski şifre doğrulaması yapmıyor** — token çalınırsa hesap ele geçirilir |
| 25 | API | `KimlikDogrulamaController.cs:163-179` | `[AllowAnonymous]` username arama — kullanıcı adı enumeration vektörü |
| 26 | MVC | `ZiyaretciModel.cs:20` | TC Kimlik No şifrelenmeden DB'de saklanıyor — KVKK riski |
| 27 | MVC | `KartOkuHub.cs:9-11` | Herhangi bir authenticated kullanıcı sahte kart okuma event'i broadcast edebilir |
| 28 | Mobil | `OkulKayitServisi.cs:29-36` | Okul listesi HTTP ile çekiliyor — MITM ile sahte okul API URL'i enjekte edilebilir |
| 29 | Mobil | `KullaniciOturum.cs:263-270` | `YetkiToken`, `RefreshToken`, `Rol` public set — herhangi bir kod değiştirebilir |
| 30 | Mobil | `AnaBaslikView.xaml.cs:77-82` | Şifre minimum 3 karakter — çok zayıf |

### DÜŞÜK

| # | Proje | Dosya | Sorun |
|---|-------|-------|-------|
| 31 | API | `GirisIstegiDto.cs:4` | `Sifre` alanında `MaxLength` yok — 10MB şifre gönderilerek bcrypt DoS yapılabilir |
| 32 | MVC | Program.cs | Login endpoint'inde rate limiting yok — brute force riski |

---

## 3. PERFORMANS PROBLEMLERİ

### YÜKSEK

| # | Proje | Dosya | Satır | Sorun |
|---|-------|-------|-------|-------|
| 33 | ~~MVC~~ | ~~`YemekhaneService.cs`~~ | ~~339-379~~ | ~~N+1 sorgu~~ — ✅ **DÜZELTİLDİ (2026-04-09):** Döngü içi 3×N sorgu → 3 batch sorgu + bellekte gruplama. Tarifeler, ay kayıtları ve ödemeler tek seferde çekilip dictionary'de eşleştiriliyor |
| 34 | ~~MVC~~ | ~~`YemekhaneService.cs`~~ | ~~415~~ | ~~Excel export `int.MaxValue`~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `pageSize: 50_000` makul üst sınır kondu, N+1 batch sorguyla artık güvenli |

### ORTA

| # | Proje | Dosya | Sorun |
|---|-------|-------|-------|
| 35 | ~~MVC~~ | ~~`ZiyaretciService.cs:229-268`~~ | ~~Pagination yok~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `GetRaporAsync`'e `pageNumber`/`pageSize` parametreleri eklendi, `Skip`/`Take` uygulandı (max 5000) |
| 36 | ~~MVC~~ | ~~`ZKTecoService.cs:142-163`~~ | ~~Backoff yok~~ — ✅ **DÜZELTİLDİ (2026-04-09):** Exponential backoff eklendi: 3s → 6s → 12s → 24s → 48s → max 60s, başarılı bağlantıda sıfırlanıyor |
| 37 | ~~API~~ | ~~`GecisKayitService.cs:47`~~ | ~~`TOP 500` hardcode~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `OFFSET/FETCH NEXT` pagination'a geçildi, `pageNumber`/`pageSize` parametreleri eklendi (max 500) |
| 38 | ~~Mobil~~ | ~~`TemelApiService.cs:26-28`~~ | ~~Ayrı HttpClient~~ — ✅ **DÜZELTİLDİ (2026-04-09):** Tüm servisler `Lazy<HttpClient>` ile tek paylaşımlı client kullanıyor, `SocketsHttpHandler` + `PooledConnectionLifetime=5dk` (DNS refresh) |
| 39 | ~~Mobil~~ | ~~`TemelApiService.cs:181`~~ | ~~Token refresh `new HttpClient()`~~ — ✅ **DÜZELTİLDİ (2026-04-09):** Statik `_refreshClient` ile paylaşımlı client, her refresh'te yeni socket açılmıyor |

---

## 4. KOD KALİTESİ

### Tekrar Eden Kod (DRY İhlali)

| Proje | Dosya | Sorun |
|-------|-------|-------|
| ~~MVC~~ | ~~`CihazService.cs:198-446`~~ | ~~ClearData fallback tekrarı~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `ClearKullaniciVerisi(zk, ip, genelTemizlikDahil)` helper metodu çıkarıldı, ~80 satır tekrar kaldırıldı |
| ~~MVC~~ | ~~`OgrencilerController.cs:339-373, 540-572`~~ | ~~Export sorgu filtresi tekrarı~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `AramaFiltreUygula(q, search, veliDahil)` helper metodu çıkarıldı, Türkçe collation + veli araması tek yerde |

### Mimari Sorunlar

| Proje | Dosya | Sorun |
|-------|-------|-------|
| ~~Mobil~~ | ~~`OgrenciListeView.cs:23`, `OgrenciDetayView.cs:35`, `AnaBaslikView.cs:95`~~ | ~~Service Locator~~ — ✅ **DÜZELTİLDİ (2026-04-09):** Constructor'lara opsiyonel service parametresi eklendi, AnaBaslikView null-safe `GetService` kullanıyor |
| ~~Mobil~~ | ~~`OgrenciListeView.cs:38-58`~~ | ~~`async void`~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `async Task LoadStudents()` olarak değiştirildi, fire-and-forget `_ =` ile çağrılıyor |
| ~~MVC~~ | ~~`HomeController.cs:9`~~ | ~~Namespace yok~~ — ✅ **DÜZELTİLDİ (2026-04-09):** `namespace OgrenciBilgiSistemi.Controllers;` eklendi |
| ~~API~~ | ~~Tüm servisler~~ | ~~`_connectionString` underscore property~~ — ✅ **DÜZELTİLDİ (2026-04-09):** 6 serviste `ConnectionString` (PascalCase) olarak standardize edildi, GirisService ile tutarlı |

---

## 5. MİMARİ DEĞERLENDİRME

### Olumlu Yönler
- MVC'de katmanlı yapı (Models → AppDbContext → Services → Controllers → Views) doğru uygulanmış
- Interface kullanımı (ICihazService, IOgrenciService vb.) DI'a uygun
- Multi-tenant yapı `TenantBaglami` ile temiz çözülmüş
- Soft-delete global query filter ile merkezi olarak yönetiliyor
- API tarafında parametreli SQL sorguları büyük ölçüde doğru kullanılmış

### Sorunlu Yönler
- **Test edilebilirlik**: Hiçbir projede test projesi yok
- **API**: ADO.NET ile raw SQL — EF Core'un tip güvenliği ve migration avantajlarından mahrum
- **Mobil**: Static `KullaniciOturum` sınıfı — test edilemez, thread-safe değil
- **Mobil**: View'larda business logic (Service Locator, inline data loading)
- **API**: `RefreshTokenService` in-memory dictionary — sunucu restart'ta tüm oturumlar silinir, multi-instance'da çalışmaz

---

## 6. ÖNCELİKLENDİRİLMİŞ İYİLEŞTİRME ÖNERİLERİ

### Acil (Bu Hafta)

**1. Tüm secret'ları rotate edin ve source control'dan çıkarın**
```
# Zaten .gitignore'a alınmış ama eski commit'lerde hala var
# JWT secret, DB şifresi, SMS credentials değiştirilmeli
# Environment variable veya Azure Key Vault kullanın
```

**2. HTTPS'e geçin** — Mobil `Constants.cs` ve `appsettings.json`:
```csharp
// Eski:
public const string VarsayilanApiUrl = "http://81.214.75.22:5196/api/";
// Yeni (SSL sertifikası aldıktan sonra):
public const string VarsayilanApiUrl = "https://api.netronyazilim.com/api/";
```

**3. Swagger'ı production'da kapatın** — `API/Program.cs`:
```csharp
// Eski:
app.UseSwagger();
app.UseSwaggerUI();

// Yeni:
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

**4. Dosya yükleme güvenliğini düzeltin** — `KullaniciService.cs` ve `KitapService.cs`'de `LocalFileStorage`'daki magic byte kontrolünü kullanın.

**5. "Beni Hatırla" şifre saklamayı kaldırın** — Refresh token ile silent re-auth yapın.

### Kısa Vadeli (2 Hafta)

**6. GuncellemeKontrolService URL düzeltmesi:**
```csharp
// Eski (404 veriyor):
var response = await _httpClient.GetAsync($"{BaseUrl}/api/uygulama-versiyon");
// Yeni:
var response = await _httpClient.GetAsync($"{BaseUrl}uygulama-versiyon");
```

**7. Şifre değiştirmede eski şifre doğrulaması ekleyin:**
```csharp
// API - GirisService.SifreDegistirAsync'e eski şifre parametresi ekleyin
// Önce mevcut hash'i DB'den okuyup PasswordHasher.VerifyHashedPassword ile doğrulayın
```

**~~8. CihazService'i Singleton yapın~~** — ✅ **YANLIŞ TESPİT:** Zaten Singleton kayıtlı (`Program.cs` satır 94).

**~~9. AidatService'e transaction ekleyin~~** — ✅ **DÜZELTİLDİ (2026-04-09):** `OdemeEkleAsync` metodu `ExecutionStrategy` + `BeginTransactionAsync` + `CommitAsync` ile sarıldı. `OdemeSilAsync`'deki mevcut pattern referans alındı.

**10. API OgrenciService.GuncelleAsync'e soft-delete filtresi:**
```sql
-- Eski:
WHERE OgrenciId = @id
-- Yeni:
WHERE OgrenciId = @id AND OgrenciDurum = 1
```

### Orta Vadeli (1 Ay)

**11.** YemekhaneService N+1 sorgusunu tek JOIN sorgusuna çevirin

**12.** `KullaniciOturum` public set'lerini `private set` yapın

**13.** Mobil `App.xaml.cs`'de static event unsubscribe ekleyin

**14.** `hash-uret` endpoint'ini production'dan silin

**15.** `RefreshTokenService`'i DB-backed yapın (veya Redis)

---

## 7. ÖZET SKOR TABLOSU

| Kategori | Yüksek | Orta | Düşük | Toplam |
|----------|--------|------|-------|--------|
| Güvenlik | **10** | 8 | 2 | 20 |
| Bug | 5 | 6 | 3 | 14 |
| Performans | 2 | 5 | 0 | 7 |
| Kod Kalitesi | 0 | 8 | 7 | 15 |
| **Toplam** | **17** | **27** | **12** | **56** |

**En kritik 3 aksiyon:** Secret rotation, HTTPS geçişi, dosya yükleme güvenliği.
