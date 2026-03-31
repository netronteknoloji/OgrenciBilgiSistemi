# OgrenciBilgiSistemi - Geliştirici Kılavuzu

Bu proje; Web Yönetim (MVC), Mobil API ve Mobil Uygulama'dan oluşan Öğrenci Bilgi Sistemidir. Tüm projeler bu repoda yönetilir.

## Teknoloji Yığını
- **Dil:** C# (.NET 9)
- **Web:** ASP.NET Core MVC (Monolith - Ana Proje)
- **API:** ASP.NET Core Web API (Mobil Backend + JWT)
- **Donanım:** ZKTeco Biometric SDK (COM Interop)
- **ORM:** Entity Framework Core (SQL Server)

## Proje Yapısı
- `OgrenciBilgiSistemi`: Ana web portalı, tüm Business ve DataAccess mantığını içinde barındırır.
- `OgrenciBilgiSistemi.Api`: Mobil uygulama için JWT tabanlı servisler.
- `OgrenciBilgiSistemi.Mobil`: .NET MAUI mobil uygulama (Android/iOS).
- `OgrenciBilgiSistemi.Shared`: Projeler arası paylaşılan enum tanımları.

## Önemli Komutlar

### Derleme ve Çalıştırma
- **Kritik:** ZKTeco SDK uyumu için projeler **x86** platformunda derlenmelidir.
- Çözümü derle: `dotnet build OgrenciBilgiSistemi.sln`
- Web Portalını çalıştır: `dotnet run --project OgrenciBilgiSistemi/OgrenciBilgiSistemi.csproj`
- API'yi çalıştır: `dotnet run --project OgrenciBilgiSistemi.Api/OgrenciBilgiSistemi.Api.csproj`

### Veritabanı İşlemleri
*Komutlar `OgrenciBilgiSistemi/` dizininden çalıştırılmalıdır.*
- Yeni Migration: `dotnet ef migrations add [Isim]`
- Veritabanı Güncelle: `dotnet ef database update`

## Kodlama Standartları

### İsimlendirme
- **Dil:** Tüm sınıf, metot, property ve değişken isimleri Türkçe olur
- **Suffix'ler değişmez:** Service, Model, ViewModel, DTO, Controller, Repository, Interface (I prefix dahil)
- **Framework isimleri değişmez:** DbContext, HttpClient, IActionResult vs.
- **Büyük/Küçük Harf:** Sınıf ve metotlarda PascalCase, değişkenlerde camelCase
- **Yorum satırları:** Türkçe yazılır
- **Proje arası tutarlılık:** MVC ve API arasında aynı isimler kullanılır

### Mimari
- **Katman sırası:** Models → AppDbContext → Services → Controllers → Views
- **Dependency Injection:** Program.cs veya Business katmanındaki Dependency Resolver üzerinden yönetilir
- **Interface:** Her Service için karşılık gelen bir Interface tanımlanır

### Asenkron Programlama
- Veritabanı ve dış kaynak işlemlerinde mutlaka async/await kullanılır
- Metot isimleri Async suffix almaz: `OgrenciGetir()`, `OgrenciGetirAsync()` değil
- Task döndüren metotlarda ConfigureAwait(false) kullanılmaz (ASP.NET Core)

### Hata Yönetimi
- İş mantığı hataları Business katmanında yakalanır
- Controller'larda try/catch yerine global exception middleware kullanılır
- Anlamlı hata mesajları Türkçe döndürülür

### Veritabanı
- Fiziksel silme yapılmaz, `IsDeleted` bayrağı kullanılır (Soft-Delete)
- Global query filter aktiftir, silinmiş kayıtlar otomatik filtrelenir
- Migration isimleri Türkçe ve açıklayıcı olur: `OgrenciTablosuEklendi`
- Tüm DB işlemleri async olur

### Güvenlik
- Web'de Cookie (8 saat), API'de JWT kimlik doğrulama kullanılır
- Yüklenen dosyalar magic bytes kontrolünden geçirilir
- Dosyalar `wwwroot/uploads` dizinine kaydedilir

### Donanım
- ZKTeco cihaz bağlantıları yalnızca `ZKTecoService` üzerinden yönetilir
- `SemaphoreSlim` ile eşzamanlılık kontrol edilir
- x86 platform zorunluluğu unutulmamalıdır

## Geliştirme Akışı
- Yeni bir özellik eklerken sırasıyla: `Models` → `AppDbContext` → `Services` → `Controllers` → `Views` adımlarını izleyin.

## Proje Yapısı ve Alt Yapılandırmalar
Bu proje hiyerarşik yapıdadır ve her katman kendi özel `CLAUDE.md` dosyasına sahiptir:
- `OgrenciBilgiSistemi/`: Ana web portalı ve iş mantığı. (Özel kurallar için `./OgrenciBilgiSistemi/CLAUDE.md` dosyasına bakınız.)
- `OgrenciBilgiSistemi.Api/`: Mobil backend ve JWT işlemleri. (Özel kurallar için `./OgrenciBilgiSistemi.Api/CLAUDE.md` dosyasına bakınız.)
- `OgrenciBilgiSistemi.Mobil/`: .NET MAUI mobil uygulama. (Özel kurallar için `./OgrenciBilgiSistemi.Mobil/CLAUDE.md` dosyasına bakınız.)