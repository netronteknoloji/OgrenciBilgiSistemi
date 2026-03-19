# Öğrenci Bilgi Sistemi — Genel Analiz Raporu

**Tarih:** 2026-03-18
**Kapsam:** OgrenciBilgiSistemi (MVC) · OgrenciBilgiSistemi.Api · StudentTrackingSystem (MAUI)

---

## 1. PROJE GENEL BAKIŞ

### Mimari

```
┌─────────────────────────────────────────────────────────────────┐
│                         SQL Server DB                           │
│                     (OgrenciBilgiSistemiDb)                     │
└────────────────────┬────────────────────────────────────────────┘
                     │
          ┌──────────┴──────────┐
          │                     │
┌─────────▼──────────┐ ┌────────▼──────────────┐
│ OgrenciBilgiSistemi│ │ OgrenciBilgiSistemi.Api│
│  (ASP.NET Core MVC)│ │  (ASP.NET Core REST)   │
│  EF Core + Cookie  │ │  ADO.NET + JWT         │
│  ZKTeco / SignalR  │ │  Swagger               │
└────────────────────┘ └────────────┬───────────┘
                                    │ HTTP/JSON + JWT
                         ┌──────────▼──────────┐
                         │ StudentTrackingSystem│
                         │   (.NET MAUI 9.0)    │
                         │  Android · iOS       │
                         └─────────────────────┘
```

### Projeler

| Proje | Tür | Platform | Kimlik Doğrulama | Veritabanı Erişimi |
|-------|-----|----------|-------------------|--------------------|
| OgrenciBilgiSistemi | MVC Web App | Windows (x86) | Cookie (8 saat) | EF Core |
| OgrenciBilgiSistemi.Api | REST API | Windows (x86) | JWT Bearer (8 saat) | Raw ADO.NET |
| StudentTrackingSystem | MAUI Mobil | Android / iOS | JWT (SecureStorage) | API üzerinden |

---

## 2. OgrenciBilgiSistemi.Api (REST API)

### Teknoloji

- .NET 9.0 · ASP.NET Core · `Microsoft.Data.SqlClient` · JWT Bearer · Swashbuckle

### Endpoint Özeti

| Method | Endpoint | Auth | Açıklama |
|--------|----------|------|----------|
| POST | `/api/auth/login` | — | Kullanıcı girişi, JWT döner |
| GET | `/api/class/all-with-count` | ✓ | Sınıf listesi + öğrenci sayısı |
| GET | `/api/students/class/{sinifId}` | ✓ | Sınıfa ait öğrenciler |
| GET | `/api/students/{id}` | ✓ | Tek öğrenci |
| GET | `/api/students/{id}/details` | ✓ | Öğrenci tam detay (veli, öğretmen, servis) |
| GET | `/api/students/attendance/{sinifId}/{dersNo}` | ✓ | Günlük yoklama durumu |
| GET | `/api/students/{id}/weekly-attendance` | ✓ | Haftalık yoklama geçmişi |
| POST | `/api/students` | Admin | Öğrenci ekle |
| PUT | `/api/students/{id}` | Admin | Öğrenci güncelle |
| DELETE | `/api/students/{id}` | Admin | Öğrenci pasife al (soft-delete) |
| POST | `/api/students/attendance/save-bulk` | Admin | Toplu yoklama kaydet (MERGE) |
| GET | `/api/gecis` | ✓ | Giriş-çıkış kayıtları (filtreli, max 500) |
| GET | `/api/gecis/{ogrenciId}` | ✓ | Öğrencinin giriş-çıkış geçmişi |
| GET | `/api/units/{id}` | ✓ | Birim detayı |

### Kimlik Doğrulama

- JWT HS256 · Issuer: `OgrenciBilgiSistemi` · Audience: `OgrenciBilgiSistemiMobile`
- Token süresi: 8 saat
- Claims: `kullaniciId`, `adminMi`, `sub`, `unique_name`, `jti`
- Şifre doğrulama: `PasswordHasher<KullaniciModel>.VerifyHashedPassword()` (C# katmanında)

### Güçlü Yönler

- Parametreli SQL sorguları (SQL enjeksiyonu yok)
- JWT ile durumsuz (stateless) kimlik doğrulama
- `AdminOnly` politikası ile yetki ayrımı
- Swagger/OpenAPI dokümantasyonu

### Zayıf Yönler / Riskler

| # | Risk | Seviye |
|---|------|--------|
| A1 | CORS: `AllowAnyOrigin` — tüm kaynaklara açık | Yüksek |
| A2 | Rate limiting / throttle yok | Orta |
| A3 | Namespace tutarsızlığı: kod `StudentTrackingSystem.Api` kullanıyor | Düşük |
| A4 | Giriş-çıkış listesi en fazla 500 kayıt döndürüyor (sessiz kesme) | Orta |
| A5 | Token yenileme (refresh token) mekanizması yok | Orta |

---

## 3. StudentTrackingSystem (MAUI Mobil Uygulama)

### Teknoloji

- .NET 9.0 MAUI · CommunityToolkit.Maui · System.Text.Json · MAUI SecureStorage

### Hedef Platformlar

Android (API 21+) · iOS (14.2+)

### Uygulama Yapısı

```
App başlangıcı
  └─ AppShell (Shell navigasyon)
       ├─ LoginView       → Giriş ekranı
       └─ ClassListView   → Sınıf listesi
            └─ StudentListView  → Öğrenci listesi
                 └─ StudentDetailView → Öğrenci detayı
```

### Servis Katmanı

| Servis | Kullandığı Endpoint'ler |
|--------|------------------------|
| `LoginService` | `POST /api/auth/login` |
| `ClassService` | `GET /api/class/all-with-count` |
| `StudentService` | `/api/students/*` (7 endpoint) |

**BaseApiService:**
- 30 saniyelik HTTP zaman aşımı
- `Authorization: Bearer {token}` otomatik ekleme
- 401 yanıtında `OnSessionExpired` eventi → login sayfasına yönlendirme
- JSON: `PropertyNameCaseInsensitive = true`

### Oturum Yönetimi

- Token + kullanıcı bilgisi `SecureStorage`'da şifreli saklanır
- 8 saatlik oturum süresi (API ile senkron)
- Uygulama yeniden açıldığında oturum süresi kontrol edilir
- **Demo modu:** App Store incelemeleri için sahte veriyle API'siz çalışır

### API URL Yapılandırması

```
Birincil:  appsettings.json (gömülü kaynak) → ApiBaseUrl
Yedek:     Constants.ApiBaseUrl (sabit değer)
Şu an:     http://81.214.75.22:5196/api/
```

> Not: URL HTTPS değil, düz HTTP kullanıyor.

### Güçlü Yönler

- Kimlik bilgileri `SecureStorage`'da güvenli şekilde saklanır
- 401 otomatik oturum temizleme ve yönlendirme
- Demo modu izolasyonu (üretim API'sine dokunmuyor)
- MVVM yapısı (`INotifyPropertyChanged`)

### Zayıf Yönler / Riskler

| # | Risk | Seviye |
|---|------|--------|
| M1 | HTTP kullanılıyor (HTTPS değil) — token açıkta gidebilir | Kritik |
| M2 | Sertifika sabitleme (certificate pinning) yok | Yüksek |
| M3 | Refresh token yok — 8 saatte bir yeniden giriş gerekiyor | Orta |
| M4 | Çevrimdışı önbellek yok — tüm veriler canlı çekilmeli | Düşük |
| M5 | Demo modu şifresiz erişilebilir | Düşük |

---

## 4. OgrenciBilgiSistemi (MVC Web Uygulaması)

### Teknoloji

- .NET 9.0 · ASP.NET Core MVC · EF Core · SignalR · ZKTeco COM (x86) · ClosedXML · Polly

### Temel Özellikler

- Cookie tabanlı kimlik doğrulama (8 saatlik kayan pencere)
- ZKTeco biyometrik cihaz entegrasyonu (COM wrapper, Singleton servis)
- SignalR ile gerçek zamanlı kart okuma olayları
- Hiyerarşik ve rol bazlı dinamik menü sistemi
- Global query filter ile aktif/pasif öğrenci ayrımı
- Excel dışa/içe aktarma (ClosedXML)

### Modül Listesi

Öğrenciler · Birimler · Kullanıcılar · Personeller · Aidat · Yemekhane · Kitaplık · Cihazlar · Ziyaretçiler · Giriş-Çıkış Takibi

### Notlar

- `PlatformTarget=x86` — ZKTeco COM bağımlılığı nedeniyle Windows zorunlu
- Veritabanı erişimi yalnızca EF Core üzerinden (ham SQL kullanılmıyor)
- API projesiyle aynı veritabanını paylaşır

---

## 5. ENTEGRASYON ANALİZİ (API ↔ MAUI)

### Veri Akışı

```
[Kullanıcı giriş yapar]
  → POST /api/auth/login
  ← JWT token (8 saat)
  → SecureStorage'a kaydedilir

[Sınıf listesi]
  → GET /api/class/all-with-count (Bearer token)
  ← List<BirimOgrenciSayisiModel>
  → ClassroomViewModel'e dönüştürülür

[Öğrenci listesi]
  → GET /api/students/class/{id}
  ← List<OgrenciModel>

[Öğrenci detayı]
  → GET /api/students/{id}/details
  ← Dictionary<string, string>  ⚠ Tip güvenliği yok

[Yoklama kaydet]
  → POST /api/students/attendance/save-bulk
  → SQL MERGE ile upsert
```

### Uyumluluk Durumu

| Endpoint | Durum | Not |
|----------|-------|-----|
| `/api/auth/login` | ✅ Uyumlu | JSON alan adları eşleşiyor |
| `/api/class/all-with-count` | ✅ Uyumlu | Ara DTO ile mapleniyor |
| `/api/students/class/{id}` | ✅ Uyumlu | `[JsonPropertyName]` ile eşleniyor |
| `/api/students/{id}/details` | ⚠ Çalışır | Dictionary — tip güvenliği yok |
| `/api/students/attendance/*` | ✅ Uyumlu | |
| `/api/gecis/*` | ℹ Eklenmemiş | API'de var, mobilde kullanılmıyor |

### Tespit Edilen Uyumsuzluklar

1. **Görsel dosya yolları tam URL değil** — API `"ahmet.jpg"` döndürüyor; mobil uygulama hangi sunucudan çekeceğini bilmiyor.
2. **Durum kodları tanımsız** — Yoklama durumları (0, 1, 2) dokümansız; mobil kodda hardcoded yorumlanıyor.
3. **`fullName` alanı yok** — API `KullaniciModel`'de `AdSoyad` dönmüyor; mobil `KullaniciAdi`'yi görünen ad olarak kullanıyor.
4. **Namespace çakışması** — API namespace'i `StudentTrackingSystem.Api`, MAUI namespace'i de `StudentTrackingSystem` — karışıklığa yol açıyor.

---

## 6. SORUNLAR VE ÖNERİLER

### Kritik

| # | Sorun | Proje | Öneri |
|---|-------|-------|-------|
| K1 | Mobil uygulama HTTP kullanıyor (token açıkta) | MAUI | `appsettings.json` → `https://` |
| K2 | CORS her kaynağa açık | API | `AllowedOrigins` kısıtlanmalı |

### Yüksek

| # | Sorun | Proje | Öneri |
|---|-------|-------|-------|
| Y1 | Refresh token yok | API + MAUI | Yenileme mekanizması eklenmeli |
| Y2 | Görsel URL'leri eksik | API | Tam URL döndürülmeli |
| Y3 | `Dictionary<string, string>` tip güvenliği yok | API | `StudentDetailsDto` sınıfı oluşturulmalı |

### Orta

| # | Sorun | Proje | Öneri |
|---|-------|-------|-------|
| O1 | Yoklama durum kodları tanımsız | API + MAUI | Enum veya sabit tanımlanmalı |
| O2 | Liste endpoint'lerinde sayfalama yok | API | `limit` / `offset` parametresi eklenmeli |
| O3 | Namespace tutarsızlığı | API | `StudentTrackingSystem.Api` → `OgrenciBilgiSistemi.Api` |
| O4 | Rate limiting yok | API | Middleware ile eklenmeli |

### Düşük

| # | Sorun | Proje | Öneri |
|---|-------|-------|-------|
| D1 | Çevrimdışı önbellek yok | MAUI | Son veri lokal önbelleğe alınabilir |
| D2 | Demo modu şifresiz | MAUI | Üretimde devre dışı bırakılmalı |
| D3 | `GET /api/gecis` mobilde kullanılmıyor | API | Giriş-çıkış ekranı eklenebilir |

---

## 7. ÖZET

### Güçlü Yönler

- Üç katmanlı mimari (MVC · API · Mobil) net sorumluluk ayrımına sahip
- JWT kimlik doğrulama doğru uygulanmış
- SQL enjeksiyonuna karşı parametreli sorgular kullanılıyor
- MAUI SecureStorage ile token güvenli saklanıyor
- Demo modu ile App Store uyumluluğu sağlanmış
- ZKTeco entegrasyonu ayrı servis katmanında izole edilmiş

### Geliştirme Önceliği

```
1. HTTPS zorunlu kılınması          (Kritik — K1)
2. CORS kısıtlanması                (Kritik — K2)
3. Refresh token mekanizması        (Yüksek — Y1)
4. Görsel URL düzeltmesi            (Yüksek — Y2)
5. StudentDetailsDto oluşturulması  (Yüksek — Y3)
6. Yoklama durum kodları tanımlanması (Orta — O1)
7. Sayfalama eklenmesi              (Orta — O2)
```
