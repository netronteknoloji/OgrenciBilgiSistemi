# CLAUDE.md - OgrenciBilgiSistemi.Shared

MVC, API ve Mobil'in paylaştığı ortak modeller ve servisler. Bağımsız class library — yalnızca `Microsoft.Extensions.{Options, DependencyInjection.Abstractions, Logging.Abstractions}` bağımlılığı.

## Sorumluluk

Sadece **ortak sözleşmeler** burada olmalı:
- Multi-tenant config modelleri (`OkulBilgiAyari`)
- Tenant context (`TenantBaglami`)
- Tenant çözümleme (`OkulYapilandirmaServisi`)
- İstek dışı tenant kapsamı (`TenantKapsamFabrikasi`, Singleton)
- Saf iş kuralları (`Kurallar/`): DB, saat ve IO bağımlılığı olmayan, MVC/API'nin ortak kullandığı kararlar — `GecisKurallari` (kart okuma), `GonderimKuyruguKurallari` (outbox tekrar deneme), `RandevuDurumKurallari` (durum geçişleri), `PanelOturumKurallari` (panel girişi + oturum denetimi), `CihazSenkronKurallari` (öğrenci hangi ZKTeco cihazında olur, senkron tekrar deneme). Testleri `OgrenciBilgiSistemi.Tests`'te; kural değişikliği testle birlikte yapılır.
- Giriş kilidi (`GirisDenemeSayaci`, saf bellek içi sayaç) ve helper'lar (`KartNo.Normalize`, `RefreshTokenBicimi`)
- POCO/DTO, enum, constant, helper/extension

Business logic, EF entity, web framework tipleri **Shared'e ait değil**.

## DI Kayıtları (MVC ve API'de aynı)

```csharp
builder.Services.Configure<List<OkulBilgiAyari>>(builder.Configuration.GetSection("Okullar"));
builder.Services.AddSingleton<OkulYapilandirmaServisi>();   // okul listesi cache
builder.Services.AddScoped<TenantBaglami>();                // per-request tenant
```

Mobil'de DB yok; `OkulBilgiAyari` tipini gerekirse import edebilir ama servisleri kaydetmez.

## Yasaklar

- ❌ EF Core veya `Microsoft.Data.SqlClient` import.
- ❌ `Microsoft.AspNetCore.*` bağımlılığı (cookie/JWT/session).
- ❌ MAUI API'leri (`Preferences`, `SecureStorage`).
- ❌ Domain entity (`*Model` EF sınıfları) — onlar MVC'de kalır.
- ❌ DB/IO yapan business logic / servis sınıfı (saf kurallar `Kurallar/` altında serbest).
- ❌ `Microsoft.AspNetCore.App` gibi büyük framework paketleri.

## Yeni Tip Ekleme Kontrol Listesi

1. Hem MVC hem API kullanıyor mu? **Hayır** → ait olduğu projeye.
2. Web framework'e bağımlı mı? **Evet** → Shared'e uygun değil.
3. DB persistence içeriyor mu? **Evet** → MVC'de EF entity olarak kalsın.
4. Sadece veri taşıyor (POCO) mu? **Evet** → uygun.

## Breaking Change Uyarısı

Sınıf/property/namespace değişikliği MVC + API + Mobil'i **aynı anda etkiler**. JSON sözleşmesini korumak için property adı değişiminde `[JsonPropertyName]` kullan, üç projeyi de derle.

## Klasörler

```
ApiContracts/   → API ↔ Mobil JSON sözleşmeleri (*Dto)
Dtos/           → API DTO'ları (GecisKayitDto, SinifYoklamaDto)
Constants/      Enums/
Helpers/        → KartNo, RefreshTokenBicimi, HaftaHesaplayici
Kurallar/       → GecisKurallari, GonderimKuyruguKurallari, RandevuDurumKurallari, PanelOturumKurallari, CihazSenkronKurallari
Models/         → OkulBilgiAyari, KartOkumaAyarlari
Services/       → TenantBaglami, OkulYapilandirmaServisi, TenantKapsamFabrikasi, GirisDenemeSayaci
```

## Naming

- Tip isimleri Türkçe (`OkulBilgiAyari`, `TenantBaglami`)
- Kod dili: Türkçe (sınıf, property, method, namespace dahil)
