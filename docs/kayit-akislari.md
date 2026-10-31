# Geçiş ve Kayıt Akışları

> Kaynak: kod okuması, commit `5eeb6fa` (2026-10-30). Log/IIS dosyalarına bakılmadı, çalışma zamanı gözlemi yok.
> Satır numaraları bu commit'e göredir. **✔** işaretli iddialar ilgili satırlar açılarak elle doğrulandı; diğerleri kod taramasıyla çıkarıldı.
> Koddan doğrulanamayan her şey **"doğrulanamadı"** olarak işaretlidir.

Kısaltmalar: `MVC/` = `OgrenciBilgiSistemi/`, `Api/` = `OgrenciBilgiSistemi.Api/`, `Mobil/` = `OgrenciBilgiSistemi.Mobil/`, `Shared/` = `OgrenciBilgiSistemi.Shared/`, `Sms/` = `OgrenciBilgiSistemi.Sms/`.
MVC servis dosyaları çoğunlukla `MVC/Services/Implementations/`, arka plan servisleri `MVC/Services/BackgroundServices/` altındadır.

---

## 1. Özet karşılaştırma

| Tür | Tetikleyici | Giriş noktası | Tablo | Tekilleştirme | Bildirim | Cihaz tarafı | Ana risk |
|---|---|---|---|---|---|---|---|
| **Ana giriş (okul kapısı)** | ZKTeco anlık olay; USB okuyucu (panel ekranı) | `KartOkumaOlayIsleyiciService.KartOkumaIsleyiciAsync`; `KartOkuController.UsbKartOkundu` | `OgrenciDetaylar` (`IstasyonTipi=AnaKapi`) | Süreç içi `GecisKilidi` + kural (10 dk, günde 1 çıkış/1 dönüş); DB unique yok | SMS (giriş+çıkış), `GECIS:{OgrenciDetayId}` | Anlık olay bağlantısı; **log okuma/silme yok** | Bağlantı kopması algılanmıyor, tek cihaza bağlanılıyor → geçiş kaybı |
| **Yemekhane** | ZKTeco anlık olay; USB; **13:30 sonrası log polling** | aynı + `YemekhanePollingService.YemekhaneLogIsle` | `OgrenciDetaylar` (`IstasyonTipi=Yemekhane`) | Anlık: kontrol+ekle (kilitsiz); polling: `sp_getapplock` | SMS (yalnız giriş), aynı anahtar | Log okuma (günlük), `ClearGLog` (ay sonu 22:00+) | İki yazma yolu birbirini kilitlemiyor; polling ChangeTracker sızıntısı |
| **Sınıf yoklaması** | Mobil (öğretmen) | `POST api/ogrenciler/attendance/save-bulk` | `SinifYoklamalar` (Ders1..8) | MERGE (öğrenci+gün), HOLDLOCK/unique yok | SMS yalnız "Gelmedi", `SINIF:{Id}:{ders}` | Yok | Yoklamanın 0 ile ezilmesi; eşzamanlı çift satır |
| **Servis yoklaması** | Mobil (servis şoförü) | `POST api/servisler/yoklama-kaydet` | `ServisYoklamalar` | MERGE (öğrenci+şoför+periyot+gün), HOLDLOCK/unique yok | SMS her durum, `SERVIS:{Id}:{durum}` | Yok | Biniş/iniş ayrımı yok (Sabah/Akşam + Bindi/Binmedi); çift satır |
| Ziyaretçi | Panel | `ZiyaretcilerController.Ekle` | `Ziyaretciler` | Yok | Yok | Yok (kart yalnız okunur) | Aynı kartla iki aktif ziyaretçi |
| Kütüphane ödünç | Panel | `KitapDetaylarController.Ekle` | `KitapDetaylar` | Önce-kontrol (`AnyAsync`) | Yok | Yok | Eşzamanlı çift kayıt |
| Randevu | Mobil (API), panel (iptal), arka plan | API `RandevularController.Olustur` | `Randevular` | Çakışma önce-kontrol | Push (API), hatırlatma (arka plan) | Yok | Panelden iptalde bildirim yok; kısmi hata |
| Öğretmen randevu slotu | Panel, mobil | `OgretmenRandevuService.Ekle` (MVC/API) | `OgretmenRandevular` | Unique `UX_OgretmenRandevular_Slot` | Yok | Yok | Örtüşen aralık yalnız önce-kontrol |
| Bildirim / Duyuru / Duyuru okuma | Panel, mobil, arka plan | `BildirimService.Olustur`, `DuyuruService` | `Bildirimler`, `Duyurular`, `DuyuruOkumalari` | Push satırı `BildirimId` unique; okuma unique | Push | Yok | Duyuru döngüsü kısmi hata → tekrar gönderimde çift push |
| Aidat / yemek ödemesi | Panel | `AidatService.OdemeEkleAsync`, `YemekhaneService.OdemeEkleAsync` | `OgrenciAidatOdemeler`, `OgrenciYemekOdemeler` | Yok | Yok | Yemek ödemesi hak vermez (`YemekHakki.HakkiOlanlar`); panelde ödeme girişi kaldırıldı | — |

Bulunamayanlar (DbSet listesi `MVC/Data/AppDbContext.cs:13-45`): personel geçişi tablosu, izin/erken çıkış tablosu, **panelden geçiş ekleme/düzeltme ekranı** (`MVC/Controllers/OgrenciGirisCikisController.cs:22-324` yalnız GET/Excel; API `GecisKayitController` `:26,69` yalnız GET).

---

## 2. Ana giriş (okul kapısı) — öğle çıkışı/dönüşü

### 2.1 Tetikleyici
- **ZKTeco anlık olay** (MVC):
  - `KartOkumaOlayIsleyiciService.StartAsync` → `ConnectAsync` (`KartOkumaOlayIsleyiciService.cs:37`), `OnCardReadAsync` aboneliği (`:40-41`). Sınıf `IHostedService`'tir (`:12`).
  - `ZKTecoService.ConnectAsync`: `Connect_Net` (`ZKTecoService.cs:83`), `RegEvent(1, 0xFFFF)` (`:90`), `OnAttTransactionEx` + `OnHIDNum` aboneliği (`:93-97`).
  - Olay başına, her handler için beklenmeyen `Task.Run` ✔ (`ZKTecoService.cs:226,247`).
  - Kayıt zamanı sunucunun `DateTime.Now`'ı (`KartOkumaOlayIsleyiciService.cs:52`); cihazın Year…Second değeri yalnız loglanır ✔ (`ZKTecoService.cs:216-218`).
- **Yeniden bağlanma:** `ZkBaglantiIzleyiciHostedService.cs:20` → `ZKTecoService.MonitorConnectionsAsync` ✔ (`:158-196`), yalnız `!IsConnected` iken, 3 sn → 60 sn üstel bekleme (`:192`).
- **USB okuyucu:** `MVC/Views/KartOku/Index.cshtml:130-158` → Enter ile `POST /KartOku/UsbKartOkundu`, 5 sn sonra abort (`:144`), input hemen temizlenir (`:140`).
- **Panelden elle giriş:** yok. `OgrenciDetaylar`'a yazan tek yerler `GecisService.cs:78,128` ve `YemekhanePollingService.cs:360`. API yalnız okur (`Api/Services/GecisKayitService.cs:58,112`, `YoneticiService.cs:50,58,221,282`).

### 2.2 Akış
**ZKTeco:**
1. `ZKTecoService.zkem_OnAttTransactionEx` / `zkem_OnHIDNum` (`ZKTecoService.cs:201-254`)
2. `KartOkumaOlayIsleyiciService.KartOkumaIsleyiciAsync` (`:50`)
3. `KartNo.Normalize` (`:53`; `Shared/Helpers/KartNo.cs:9-16`)
4. `TenantKapsamFabrikasi.Olustur(BagliOkulKodu)` (`:57,66`)
5. Cihaz seçimi: bağlı cihaz; `CurrentCihazId` null ise okulun ilk ZKTeco cihazı (`:73-78`)
6. `KartOkuService.GetOgrenciByKartNoAsync` (`:87`; `KartOkuService.cs:19-28` ✔)
7. `GecisKilidi.AlAsync(okulKodu, ogrenciId)` (`:95`)
8. `GecisKarariService.KararVerAsync` (`:97`; `GecisKarariService.cs:20-49`) → `KartOkuService.BugunAnaKapiHareketleriAsync` (`KartOkuService.cs:56-76`) → `GecisKurallari.KararVer` (`Shared/Kurallar/GecisKurallari.cs:58`)
9. Red → yalnız SignalR yayını (`:98-103`)
10. Kabul → `GecisService.KaydetAsync(..., karar.ZorunluGecisTipi)` (`:107`)
11. `GonderimKuyruguService.GecisSmsleriniKuyrugaAlAsync(ogrenciId)` + `KuyrukTetikleyici.Tetikle()` (`:116-118`)
12. `KartOkumaYayini.GonderAsync` (`:128`; `MVC/Hubs/KartOkumaYayini.cs:11-24`, 3 sn zaman aşımı)

**USB:** `KartOkuController.UsbKartOkundu` (`KartOkuController.cs:74-76`) → `CihazService.CihazBulAktifByKodAsync` (`:87`) → `KartNo.Normalize` (`:92`) → `GetOgrenciByKartNoAsync` (`:96`) → `GecisKilidi.AlAsync` (`:118`) → `KararVerAsync` (`:123`) → `GecisService.KaydetAsync` (`:133`) → SignalR (`:140`) → SMS kuyruğu (`:148-149`). Yayın/SMS sırası ZKTeco yolunun tersidir.

### 2.3 Kurallar
- **Pencere:** config `MVC/appsettings.json:9-13` (`KartOkuma`: 11:30–13:30, `OgleDonusMinDakika`=10); geçersiz değerde varsayılan (`Shared/Models/KartOkumaAyarlari.cs:11-30`), uçlar dahil (`:24`).
- **Karar** (`GecisKurallari.cs:69-101` ✔):
  - Öğle izni `Hayir` → red (`ANA_KAPI_OGLE_RED`). Varsayılan izin `Hayir` ✔ (`MVC/Models/OgrenciModel.cs:37`).
  - Bugün çıkış yok → pencere içiyse `ÇIKIŞ`, değilse red (`ANA_KAPI_OGLE_SAAT_DISI`).
  - Çıkış var, dönüş yok → 10 dk dolmadıysa red, aksi halde `GİRİŞ`.
  - İkisi de var → red (`ANA_KAPI_OGLE_DONUS_LIMIT`).
  - **Sonuç:** ana kapı yalnız öğle çıkışı/dönüşünü kaydeder; **sabah girişi / akşam çıkışı kaydı yoktur.**
- **"Bugün" verisi:** ilk ana kapı çıkışı ve sonraki giriş (`KartOkuService.cs:59-75`); istasyon tipi kaydın alanından değil cihazın güncel `IstasyonTipi`'nden (`:62,72`).
- **Yön:** kural yön verir (`ZorunluGecisTipi`); vermezse `GecisService` son kaydı açık giriş mi diye bakıp çevirir (`GecisService.cs:84-112`).
- **Tekilleştirme:** süreç içi `GecisKilidi` (`MVC/.../GecisKilidi.cs:10-25`) + 10 dk kuralı. `OgrenciDetaylar`'da unique index yok (`AppDbContext.cs:102-114`).
- **Pasif öğrenci:** `!IsDeleted` ✔ (`KartOkuService.cs:27`); `GecisService` exception fırlatır (`GecisService.cs:32-37`).
- **Bilinmeyen kart:** ZKTeco yolunda yalnız log (`KartOkumaOlayIsleyiciService.cs:88-92`); USB'de `KART_TANIMSIZ` yayını + 404 (`KartOkuController.cs:98-115`).

### 2.4 Tablo
`OgrenciDetaylar` / `MVC/Models/OgrenciDetayModel.cs`: `OgrenciId` (`:17`), `IstasyonTipi` (`:22`), `OgrenciGTarih`/`OgrenciCTarih` (`:24-25`), `OlayZamani` persisted computed COALESCE (`:30`; `AppDbContext.cs:104-105`), `OgrenciGecisTipi` "GİRİŞ"/"ÇIKIŞ" nvarchar(5) (`:35`), `OgrenciSmsGonderildi` (`:37`), `CihazId` (`:43`). Global filtre `!Ogrenci.IsDeleted` (`AppDbContext.cs:97-98`). Yazım ReadCommitted transaction + execution strategy (`GecisService.cs:42-45,128-131`).

### 2.5 Bildirim
- `GonderimKuyruguService.GecisSmsleriniKuyrugaAlAsync` (`GonderimKuyruguService.cs:27-99`): `OgrenciSmsGonderildi != true`, istasyon AnaKapi/Yemekhane, **yalnız bugün**, veli telefonu dolu (`:36-41`); SMS ayarı kapalıysa hiçbir şey (`:30`). Ana kapıda giriş ve çıkış (`:73-75`).
- `TekilAnahtar = GECIS:{OgrenciDetayId}` (`Shared/Kurallar/GonderimKuyruguKurallari.cs:24`), unique filtreli index (`AppDbContext.cs:564-567`).
- Bayrak UPDATE + kuyruk INSERT aynı transaction (`GonderimKuyruguService.cs:103-126`); **geçiş kaydı ise ayrı, önceden commit edilmiş transaction**. Aradaki hata yutulur (`KartOkumaOlayIsleyiciService.cs:120-124`; `KartOkuController.cs:151-155`).
- Güvenlik ağı: `GonderimKuyruguIsleyici` dakikada bir aynı taramayı yapar (`GonderimKuyruguIsleyici.cs:24,84`).
- Push yok.

### 2.6 Cihaz tarafı
- Anlık olay bağlantısı `ZkCihazKilidi` **dışında**; yalnız connect/disconnect'i koruyan `_connLock` (`ZKTecoService.cs:19,44,122`).
- Ana kapı cihazından **log okuma ve log silme yok**. Cihaz saat senkronu (`SetDeviceTime`) repo genelinde yok.
- Kullanıcı yazma/silme yalnız `CihazSenkronIsleyici` (bkz. §7.2): ana kapıda her aktif öğrenci cihazda olmalı (`Shared/Kurallar/CihazSenkronKurallari.cs:28-34`); cihaz kullanıcı Id = `OgrenciId` ✔ (`CihazSenkronIsleyici.cs:242,290`); `SetStrCardNumber`+`SetUserInfo` (`:289-291`), silme `DeleteEnrollData`/`SSR_DeleteEnrollData` (`:263-264`), kart başka kullanıcıdaysa onun kartı boşaltılır (`:276-286`).
- Panelden "tüm kullanıcıları sil" `ClearData(1)` ile **attendance loglarını da siler** (`CihazService.cs:94,386-393`).

### 2.7 Riskler
1. **Bağlantı kopması algılanmıyor ✔.** `IsConnected=false` yalnız `DisconnectAsync` içinde (`ZKTecoService.cs:141`); `DisconnectAsync`'i çağıran kod yok (yalnız tanım `:120` ve arayüz). `OnDisConnected` aboneliği/heartbeat yok. Kopan bağlantıda `MonitorConnectionsAsync` hiç devreye girmez, olaylar sessizce durur. Ayrıca `DisconnectAsync` `_internalCardHandler=null` yapar (`:142`), abonelik yalnız `StartAsync`'te kurulduğu için yeniden bağlansa bile kayıt yazılmaz.
2. **Tek cihaza bağlanılıyor ✔.** Tüm okullar dolaşılıp `CihazId` sırasına göre ilk ZKTeco cihazı seçiliyor, istasyon tipine bakılmıyor (`ZKTecoService.cs:53-71`). Diğer okulların ve diğer ana kapı cihazlarının anlık olayları alınmaz; ana kapı için telafi eden log okuma da yok.
3. **`OnAttTransactionEx` kullanıcı Id'sini kart no gibi arıyor ✔.** `EnrollNumber` → `kartNo` (`ZKTecoService.cs:214`) → `OgrenciKartNo == k` (`KartOkuService.cs:27`). Cihazdaki kullanıcı Id `OgrenciId` olduğundan bu olay normalde "tanımsız" düşer; bir öğrencinin Id'si başka bir öğrencinin kart numarasına eşitse **yanlış öğrenciye kayıt** yazılır. Gerçek kart `OnHIDNum(int)` ile gelir (`:236-238`). Cihazın her okutmada `OnHIDNum` gönderip göndermediği ve `int` taşması — **doğrulanamadı**.
4. **Olay hatası kalıcı kayıp:** DB/servis hatası yalnız loglanır (`KartOkumaOlayIsleyiciService.cs:133`); cihaz/okul bulunamazsa olay düşer (`:57-61,80-84`).
5. COM nesnesi thread-pool'da oluşturuluyor (`ZKTecoService.cs:81`), `STAThread`/mesaj pompası yok — olayların bu ortamda güvenilir gelip gelmediği **doğrulanamadı**.
6. Tek okutma iki olay (`OnAttTransactionEx` + `OnHIDNum`) → iki paralel `Task.Run` (`:222-253`). Kilit sıraya sokar, ikincisi kuralla reddedilir; Risk 3 ile birleşirse yanlış öğrenci.
7. `GecisKilidi` yalnız süreç içi ve sözlük temizlenmiyor (`GecisKilidi.cs:8,12,16`). IIS overlapped recycle'da iki worker'ın aynı anda bağlı kalması — **doğrulanamadı**.
8. USB'de istemci tarafı çift gönderim koruması yok (`Index.cshtml:130-158`); 5 sn abort transaction ortasına denk gelirse kayıt yazılmaz, sonrasına denk gelirse SMS adımı atlanır (güvenlik ağı 1 dk içinde telafi eder) (`KartOkuController.cs:151,165-168`).
9. `CurrentCihazId` null iken olay okulun ilk ZKTeco cihazına (yanlış istasyon olabilir) yazılır (`KartOkumaOlayIsleyiciService.cs:73-78`); istasyon tipi kurallarda cihazdan (`KartOkuService.cs:47,62`), `GecisService`'te kayıttan (`GecisService.cs:54`) okunur — cihaz tipi değişirse tutarsızlaşır.

---

## 3. Yemekhane

### 3.1 Tetikleyici
- **Anlık / USB:** ana kapıyla aynı yol (§2.1–2.2). ZKTeco anlık olayı yalnız yemekhane cihazı "ilk ZKTeco cihazı" ise alınır (§2.7 Risk 2).
- **Polling:** `YemekhanePollingService` (`MVC/Program.cs:122`):
  - Döngü dakikada bir (`YemekhanePollingService.cs:25,76`), yalnız **13:30 sonrası** — sabit kod (`:27,67`).
  - Tüm okullar: `HerOkulIcinAsync` (`:68`).
  - Cihaz başına durum (`BasariliGun`, `TemizlikGunu`, `SonDeneme`) **bellekte** (`:38-45`); bugün okunmamışsa veya temizlik günüyse ve son denemeden 5 dk geçtiyse işlenir (`:106-116`).

### 3.2 Akış (polling)
1. `YemekhaneLogIsle` (`:86`): yemekhane ZKTeco cihazları (`:96-103`), aktif öğrenci Id kümesi (`:121-123`)
2. Cihaz başına `ZkCihazKilidi.AlAsync` (`:133`)
3. `CihazdanLogCek` (`:135`; `:232-326`)
4. `LoglariYaz` (`:138`; `:152-226`) → kayıt başına `TekilYemekhaneEkle` (`:189`; `:331-371`)
5. Bugün yeni kayıt varsa `GecisSmsleriniKuyrugaAlAsync(null)` (`:215`)
6. Koşullar sağlanırsa `CihazLogTemizle` (`:142`; `:376-412`)

### 3.3 Kurallar
- **Anlık yol** (`GecisKurallari.cs:62-67`; `GecisKarariService.cs:27-35`):
  - Bu ay izin yoksa red. İzin = tek tanım `YemekHakki.HakkiOlanlar`: o ayın `OgrenciYemekler.Aktif` kaydı, silinmiş öğrenci hariç; ödeme hak vermez (`YemekHakki.cs:13`; `KartOkuService.cs:31-32`).
  - Bugün kayıt varsa red (`YEMEKHANE_LIMIT`) (`KartOkuService.cs:42-50`).
  - `GecisService` ayrıca günde tek GİRİŞ uygular, varsa mevcudu döndürür (`GecisService.cs:49-82`) — **applock yok**.
- **Polling yolu:**
  - Yemek hakkı filtresi **yok** (cihaz kapıyı açtığı için geçiş yazılır), yalnız mevcut öğrenci kontrolü. Hakkı olmayan öğrenciye ait yeni kayıtlar `HaksizGecis` olarak sayılır ve Warning loglanır (`YemekhanePollingService.cs:194,214`).
  - Geçersiz ve gelecek tarihli kayıtlar atlanır (`:275-283`); saniye 0, saat = cihaz saati (`:281`).
  - Öğrenci+gün için günün ilk kaydı (`:156-159`); DB'deki mevcut GİRİŞ'ler önceden yüklenir (`:161-175`).
  - Ekleme `sp_getapplock` `YEMEKHANE:{OgrenciId}:{yyyyMMdd}`, 5 sn (`:338-344`) + içeride tekrar varlık kontrolü (`:346-358`).
- **Yön:** her zaman GİRİŞ, çıkış kaydı yok (`GecisService.cs:47-48`; `YemekhanePollingService.cs:294`).

### 3.4 Tablo
`OgrenciDetaylar`, `IstasyonTipi=Yemekhane`, `OgrenciGecisTipi="GİRİŞ"`, `OgrenciCTarih=null`, `OgrenciSmsGonderildi=false` (`YemekhanePollingService.cs:288-297`; `GecisService.cs:68-76`).

### 3.5 Bildirim
- Yalnız giriş için SMS (`GonderimKuyruguService.cs:64`), anahtar `GECIS:{OgrenciDetayId}`.
- Polling yalnız **bugün** yeni kayıt varsa kuyruğa alır (`YemekhanePollingService.cs:211-216`) ve tarama da bugünle sınırlı (`GonderimKuyruguService.cs:39-40`) → önceki günlerden tamamlanan kayıtlar **SMS'siz** kalır.
- Polling 13:30'dan önce çalışmadığından ZKTeco anlık olayı alınmayan yemekhane SMS'i en erken 13:30'da çıkar.

### 3.6 Cihaz tarafı
- **Kilit:** `ZkCihazKilidi` (ip:port, süreç içi, `ZkCihazKilidi.cs:9`) altında polling (`:133`), senkron işleyici (`CihazSenkronIsleyici.cs:148`), panel işlemleri (`CihazService.cs:79,130`).
- **Okuma:** `Connect_Net` → `EnableDevice(false)` → `ReadGeneralLogData`; `false` dönerse hata, temizlik yapılmaz, sonra tekrar denenir (`:256-261`) → `GetGeneralLogData` döngüsü → `finally` `EnableDevice(true)` + `Disconnect` (`:242-324`). Her seferinde cihazdaki **tüm** loglar okunur (`:229`).
- **Silme (`ClearGLog`, `:390`)** — hepsi gerekli: ayın son günü ve 22:00 sonrası (`:93-94`); o kilit oturumunda okuma başarılı (`:136`); DB yazımı hatasız (`:138`); aynı gün daha önce temizlenmemiş (`:112,142-143`).
- **Kullanıcılar:** yalnız bu ay yemek hakkı olan (`YemekHakki.HakkiOlanlar`) aktif öğrenciler (`CihazSenkronKurallari.cs:32`; `CihazSenkronIsleyici.cs:124`). Kuyruğa ekleyenler: Yemekhane sayfasındaki hak değişiklikleri (tekil/toplu/Excel, yalnız içinde bulunulan ay), MVC `OgrenciService`, panel "Eşitle" (`CihazlarController.cs:35`) ve Yemekhane sayfası "Cihazla Eşitle" (`YemekhaneController.cs:136` → `CihazSenkronService.YemekhaneCihazlariniEsitleAsync`, yalnız yemekhane cihazları).

### 3.7 Riskler
1. **Polling'de ChangeTracker temizlenmiyor ✔.** `TekilYemekhaneEkle` hata alınca rollback edip fırlatır (`:360-369`), `LoglariYaz` yakalayıp devam eder (`:199-203`); dosyada `ChangeTracker.Clear` yok. `Added` durumdaki başarısız entity aynı `DbContext`'te kalır, sonraki `SaveChangesAsync` onu applock dışında yeniden eklemeye çalışır → çift kayıt veya art arda hata.
2. **İki yazma yolu birbirini kilitlemiyor:** anlık yol applock almaz (`GecisService.cs:49-82`), polling `GecisKilidi` almaz (`:331-371`), DB unique yok → 13:30 sonrası eşzamanlıysa iki satır + iki SMS. Yemekhane cihazının anlık olay kanalında olup olmadığı **doğrulanamadı** (Risk §2.7-2'ye bağlı).
3. **Ay sonu temizliği ayrı bağlantıda:** okuma bağlantısı kapanır (`:315-316`), `CihazLogTemizle` yeni bağlantıyla `EnableDevice(false)` yapmadan `ClearGLog` çağırır (`:383,390`); aradaki okutma okunmadan silinebilir (22:00 sonrası, olasılık düşük).
4. ~~Hak tanımı tutarsız~~ — giderildi: anlık yol, senkron ve polling aynı tanımı (`YemekHakki.HakkiOlanlar`) kullanır; ödeme hak vermez. Polling hakkı olmayan geçişi yazar ama sayar/uyarır.
5. **Ay geçişinde senkron tetiklenmez (bilinçli):** ay başı otomatik eşitleme yok; ayın 1'inde Yemekhane → "Cihazla Eşitle" (veya Cihazlar → Eşitle) elle basılır.
6. **Saat senkronu yok:** cihaz saati ileriyse kayıt "gelecek" diye atlanır (`:283`), ay sonu temizliğinde kalıcı kaybolur.
7. **Durum bellekte:** 22:00–24:00 arası yeniden başlatma o ayın temizliğini atlatabilir (`:38-45`); okuma tekrarları tekilleştirme sayesinde zararsız.
8. **API'den eklenen/kartı değişen öğrenci cihaza gitmez ✔:** `Api/Services/OgrenciService.cs:493` (`EkleAsync`), `:535` (`GuncelleAsync`); API'de `CihazSenkron` geçen satır yok. Kart cihaza ulaşmaz (ana kapı için de geçerli).

---

## 4. Sınıf yoklaması

### 4.1 Tetikleyici
- **Mobil (öğretmen):** `Mobil/Views/OgrenciListeView.xaml:277,281` → `Mobil/ViewModels/OgrenciListeGorunumModel.cs:82-113` ✔.
- Panelden yazma yok; MVC yalnız okur (`GecisRaporService.cs:184,269,290`).

### 4.2 Akış
1. `OnAppearing` → `YukleAsync`: öğrenciler `GET api/ogrenciler/class/{sinifId}`, tüm durumlar **0** ✔ (`OgrenciListeGorunumModel.cs:54-56`).
2. Ders seçimi → `PeriyotDegistirAsync` → `MevcutYoklamaGetirAsync` (`Mobil/Services/OgrenciService.cs:115-120`); kayıt varsa mevcut durum, yoksa **1 (Geldi)** ✔ (`:77`); hata `catch { }` ✔ (`:79`).
3. `YoklamaIsleAsync` → `TopluYoklamaKaydetAsync` → `POST api/ogrenciler/attendance/save-bulk` (`Mobil/Services/OgrenciService.cs:135-159`).
4. `Api/Controllers/OgrencilerController.cs:308-360` → `Api/Services/OgrenciService.cs:310-365`: transaction, öğrenci başına MERGE ✔ (`:321-357`).
5. Commit sonrası `_yoklamaSms.SinifYoklamaBildir` (`OgrencilerController.cs:340-347`), hata yutulur.

### 4.3 Kurallar
- Yalnız `rol == "Ogretmen"` (`OgrencilerController.cs:311-313`); `KullaniciId` token'dan (`:315-317`). Öğretmen–sınıf eşleşmesi kontrol edilmez (bilinçli kural, kök `CLAUDE.md`); MERGE yalnız öğrencinin o sınıfta ve silinmemiş olmasına bakar ✔ (`Api/Services/OgrenciService.cs:328-329`).
- **Tekilleştirme:** öğrenci başına günde tek satır, 8 ders `Ders1..Ders8`. MERGE eşleşmesi `OgrenciId + CAST(OlusturulmaTarihi AS DATE) = bugün` ✔ (`:326-339`). UPDATE'te `KullaniciId` değişmez (`:332-336`) → "yoklamayı yapan" günün ilk öğretmeni kalır.
- Ders 1–8 (`:316-317`); saat penceresi yok; tarih her zaman DB `GETDATE()` (geçmişe yazılamaz).
- **`DurumId` sunucuda doğrulanmaz ✔** (`Api/Dtos/YoklamaKayitOgesiDto.cs:7`, düz `int`). Değerler 1=Geldi, 2=Gelmedi, 3=GeçGeldi, 4=İzinli, 5=Raporlu, 6=Nöbetçi, 7=Görevli (`Shared/Enums/YoklamaDurumu.cs:5-11`).
- Düzeltme = aynı uca yeniden POST ("Güncelle", onaylı, `OgrenciListeGorunumModel.cs:85-91`). Silme yolu yok.

### 4.4 Tablo
`SinifYoklamalar` (`MVC/Models/SinifYoklamaModel.cs:11-41`): `OgrenciId`, `KullaniciId`, `Ders1..8 int?`, `SmsDurumu` (ders başına bit), `OlusturulmaTarihi`, `GuncellenmeTarihi`. İndeksler unique değil (`AppDbContext.cs:335`; `OlusturulmaTarihi` datetime2 olduğundan zaten gün tekilliği sağlayamaz).

### 4.5 Bildirim
- Yalnız **DurumId=2 (Gelmedi)** (`Api/Services/YoklamaSmsBildirimService.cs:45-50`; sorgu `Ders{n}=2` ve SMS biti 0, `:129-130`). Geç geldi / izinli vb. için SMS yok. Push yok.
- Koşullar: `SmsAyarlari.Aktif` (`:114`), veli telefonu (`:132`). Veli bazında SMS tercihi **bulunamadı**.
- Kuyruğa alma yoklama transaction'ından **ayrı**, öğrenci başına kendi transaction'ı: bayrak UPDATE, `IF @@ROWCOUNT=1 AND NOT EXISTS(... UPDLOCK, HOLDLOCK)` INSERT (`:169-208`); hata → 0, bayrak set edilmez, sonraki taramada yeniden denenir.
- Anahtar `SINIF:{SinifYoklamaId}:{dersNo}` (`GonderimKuyruguKurallari.cs:25`) — **durum anahtarda yok**.
- Güvenlik ağı: API `YoklamaSmsKuyrukTarayici` dakikada bir (`Api/.../YoklamaSmsKuyrukTarayici.cs:12,31-36`), yalnız bugün (`YoklamaSmsBildirimService.cs:211-215`).
- MERGE durum değişince ilgili SMS bitini temizler ✔ (`Api/Services/OgrenciService.cs:334-336`).

### 4.6 Cihaz tarafı
Yok.

### 4.7 Riskler
1. **Yoklamanın 0 ile ezilmesi ✔ (kod okumasıyla; cihazda denenmedi).** `YukleAsync` her `OnAppearing`'de tüm durumları 0 yapar (`:56`), seçili ders korunduğu için `PeriyotDegistirAsync` yeniden çalışmaz; `PeriyotDegistirAsync` ağ hatasında da sessizce biter (`:79`). `YoklamaIsleAsync`'te 0 kontrolü yok (`:93-107`; serviste var: `ServisEkraniGorunumModel.cs:109`), sunucu da doğrulamıyor → tüm sınıf `Ders{n}=0` yazılabilir. Hangi gezinmenin `OnAppearing`'i yeniden tetiklediği (ör. öğrenci detayına gidip dönme, `:119`) cihazda **doğrulanamadı**.
2. **Eşzamanlı çift satır:** MERGE'de `HOLDLOCK`/`SERIALIZABLE` yok ✔ (Api `OgrenciService.cs`'te `HOLDLOCK` geçmiyor), unique index yok → aynı öğrenciye aynı anda iki istek (iki öğretmen, timeout sonrası yeniden gönderim) iki satır açabilir → farklı `SinifYoklamaId` → **iki SMS**, özet sorgusunda öğrenci iki kez (`:223-226`).
3. **Düzeltme SMS'i yok / yanlış SMS geri alınamaz:** anahtar durumu içermediği için Gelmedi→Geldi→Gelmedi'de ikinci SMS çıkmaz; gönderilmiş "devamsız" SMS'i iptal eden kod yok.
4. **Varsayılan "Geldi":** kaydı olmayan öğrenci 1 ile gelir (`:77`), fark edilmezse yanlış devam kaydı.
5. **Saat dilimi:** kayıt/“bugün” DB `GETDATE()` (`:326,333,339`), SMS taraması API yerel saati (`YoklamaSmsBildirimService.cs:172,213`). Sunucu saat dilimleri **doğrulanamadı**; farklıysa gün sınırında SMS kaçar.
6. **Offline kuyruk yok:** başarısızlıkta yalnız uyarı (`:108-111`), HttpClient timeout 30 sn (`Mobil/Services/TemelApiService.cs:20`). Çift dokunma `[RelayCommand]` varsayılan davranışıyla engellenir (kütüphane varsayılanı, kodda açık ayar yok).
7. Öğrenci gün içinde sınıf değiştirirse aynı günlük satır kullanılır (MERGE'de sınıf yok).

---

## 5. Servis yoklaması (biniş)

### 5.1 Tetikleyici
Mobil (servis şoförü): `Mobil/Views/ServisEkraniView.xaml:187,191` → `Mobil/ViewModels/ServisEkraniGorunumModel.cs:86-131`. Panelden yazma yok (`ServisRaporController.cs:20,55` yalnız GET; `ServisRaporService.cs:95` okur).

### 5.2 Akış
1. `YukleAsync`: servis kimliği `KullaniciOturum.ServisId`, durumlar 0 (`:43-67`).
2. Periyot seçimi → `GET api/servisler/{servisId}/yoklama/{periyot}` (`:69-84`, `catch { }`).
3. Seçilmemiş (0) öğrenci varsa gönderim engellenir ✔ (`:109-115`).
4. `Mobil/Services/ServisService.cs:106-128` → `POST api/servisler/yoklama-kaydet`.
5. `Api/Controllers/ServisController.cs:84-127` → `Api/Services/ServisService.cs:185-235` (transaction, öğrenci başına MERGE).
6. `_yoklamaSms.ServisYoklamaBildir` (`ServisController.cs:111-118`), hata yutulur.

### 5.3 Kurallar
- Yalnız `rol == "Servis"`, `servisId` token'dan (`ServisController.cs:87-93`); MERGE kaynağı `O.ServisId = @kullaniciId` → şoför yalnız kendi servisindeki öğrenciye yazar (`Api/Services/ServisService.cs:200`); okuma uçları `ServisErisimKontrol` (`ServisController.cs:174-185`).
- MERGE anahtarı `OgrenciId + KullaniciId + Periyot + gün` (`:201-204`).
- **Biniş/iniş ayrımı yoktur.** Periyot 1=Sabah, 2=Akşam; Durum 1=Bindi, 2=Binmedi (`MVC/Models/ServisYoklamaModel.cs:28-34`). `[Range(1,2)]` DTO doğrulaması var.

### 5.4 Tablo
`ServisYoklamalar` (`MVC/Models/ServisYoklamaModel.cs:11-39`): `OgrenciId`, `KullaniciId`, `DurumId`, `Periyot`, `SmsGonderildi`, tarihler. Index `(KullaniciId, OgrenciId, Periyot, OlusturulmaTarihi)` unique değil (`AppDbContext.cs:361`).

### 5.5 Bildirim
- Her durum (Bindi ve Binmedi) için SMS (`YoklamaSmsBildirimService.cs:40-43`), filtre `SmsGonderildi = 0` (`:76`), `SmsAyarlari.Aktif` (`:64`) + veli telefonu (`:78`).
- Anahtar `SERVIS:{ServisYoklamaId}:{DurumId}` (`GonderimKuyruguKurallari.cs:26`); durum değişince MERGE `SmsGonderildi=0` (`Api/Services/ServisService.cs:208`).
- Kuyruğa alma ve güvenlik ağı §4.5 ile aynı mekanizma.

### 5.6 Cihaz tarafı
Yok.

### 5.7 Riskler
1. MERGE'de `HOLDLOCK` yok ✔, unique yok → eşzamanlı çift satır → çift SMS.
2. A→B→A durum dönüşünde üçüncü değişikliğin SMS'i `NOT EXISTS` ile bastırılır (`YoklamaSmsBildirimService.cs:174`); yanlış SMS iptal edilemez.
3. Saat dilimi farkı (§4.7-5 ile aynı; `ServisService.cs:119,197,207,211`).

---

## 6. Diğer kayıt türleri

| Tür / Tablo | Tetikleyici → yazan | Önemli alanlar / tekilleştirme | Bildirim | Risk |
|---|---|---|---|---|
| Ziyaretçi / `Ziyaretciler` | Panel: `ZiyaretcilerController.Ekle` (`:96-123`) → `ZiyaretciService.EkleAsync` (`:24-40`); `GuncelleAsync` (`:42`), `CikisYapAsync` (`:56`), `SilAsync` (`:78`, `AktifMi=false`) | `GirisZamani`, `CikisZamani`, `AktifMi`, `KartNo`, `CihazId`. Unique yok; `KartNo.Normalize` uygulanmıyor; kart okutma yalnız okur (`KartOkutAsync` `:182`) | Yok | Aynı kartla iki aktif ziyaretçi |
| Kütüphane / `KitapDetaylar` | Panel: `KitapDetaylarController.Ekle` (`:58`) → `KitapDetayService.AddAsync` (`:135-157`), `TeslimAlAsync` (`:199`) | `KitapDurum`, `KitapAlTarih`, `KitapVerTarih`; önce-kontrol `AnyAsync` (`:139`) | Yok | Eşzamanlı çift ödünç |
| Randevu / `Randevular` | API `RandevularController.Olustur` (`:63`) → `RandevuService.RandevuEkle` (`:310-333`, çakışma `:197`); durum `:245,266` (koşullu UPDATE); panel iptal MVC `RandevuService.cs:41-52`; `RandevuArkaPlanService` (`:43-106`) | `Durum`, `RandevuTarihi`, `SureDakika`, `IsDeleted`; unique değil (`AppDbContext.cs:425-428`) | Push: `RandevularController.cs:99,132,166,234`; hatırlatma `RandevuArkaPlanService.cs:91,97`; **panel iptalinde yok** | Randevu ve bildirim ayrı transaction (`RandevularController.cs:83-102`); hatırlatma kontrolü alıcıya bakmaz → veli atlanabilir (`:82-101`) |
| Öğretmen slotu / `OgretmenRandevular` | MVC `OgretmenRandevuService.Ekle` (`:37-49`), API (`:47-80`) | Unique `UX_OgretmenRandevular_Slot` (`AppDbContext.cs:443-447`); örtüşme önce-kontrol | Yok | Örtüşen aralık yarışı |
| Bildirim / `Bildirimler` | `BildirimService.Olustur` (MVC `:26-43`, API `:22-49`) | Push kuyruk satırıyla aynı transaction; `UX_GonderimKuyrugu_BildirimId` (`AppDbContext.cs:570-573`) | Push | Push kısmi başarı (§7.1) |
| Duyuru / `Duyurular` | MVC `DuyuruService` (`:34-42`), API `OgretmenDuyuruOlustur` (`:22-83`) | Hedef veli başına ayrı `Olustur` | Push | Döngü ortada patlarsa duyuru kalır, ilk N veli push almıştır; yeniden gönderimde çift push |
| Duyuru okuma / `DuyuruOkumalari` | API `OkunduIsaretle` (`:138-176`, `INSERT … WHERE NOT EXISTS`) | Unique (DuyuruId, KullaniciId) (`AppDbContext.cs:520-523`) | Yok | — |
| Aidat ödemesi / `OgrenciAidatOdemeler` | Panel `AidatController.OdemeEkle` (`:260`) → `AidatService.OdemeEkleAsync` (`:402-449`); silme `:293` | Unique değil | Yok | — |
| Yemek ödemesi / `OgrenciYemekOdemeler` | Panel `YemekhaneController.OdemeEkle` (`:147`) → `YemekhaneService.OdemeEkleAsync` (`:166-180`) | Unique değil (`AppDbContext.cs:181`) | Yok | Cihaz senkronunu tetiklemez (§3.7-4) |
| SMS geçmişi / `SmsGonderimGecmisleri` | `GonderimKuyruguIsleyici.cs:158-171` | Her deneme bir satır | — | — |
| Push token / `BildirimCihazlari`; oturum / `RefreshTokenlar` | Mobil: `RawSqlBildirimTokenDeposu.cs:20` (MERGE); `RefreshTokenService.cs:39` | FcmToken unique (`:641-643`); TokenHash unique (`:618-619`) | — | — |

---

## 7. Ortak altyapı

### 7.1 SMS/push outbox — `GonderimKuyrugu`
- **Kurallar** (`Shared/Kurallar/GonderimKuyruguKurallari.cs`): MaksDeneme 10 (`:18`), parti 50 (`:19`), kilit 5 dk (`:20`), saklama 30 gün (`:21`); anahtarlar (`:24-26`); SMS geçerliliği gün sonu (`:29`), push 1 gün (`:31`); bekleme 1/2/4/8/15/30 dk (`:34-42`); `SonrakiAdim` (`:45-63`).
- Durumlar: Bekliyor 0, Gonderiliyor 1, Gonderildi 2, Basarisiz 3, SuresiDoldu 4 (`Shared/Enums/GonderimKuyrugu.cs:11-18`). Model `MVC/Models/GonderimKuyruguModel.cs:11-53`.
- Unique: `UX_GonderimKuyrugu_TekilAnahtar` (filtre `IS NOT NULL`), `UX_GonderimKuyrugu_BildirimId` (`AppDbContext.cs:564-573`; migration `20261011130339_GonderimKuyrugu.cs:53-64`).
- **Üreticiler:** MVC `GonderimKuyruguService` (`:27-126`; duplicate key özel işlenmez, genel `catch` `:87-91`), `PushEkle` (`:128-142`, commit çağıranın `SaveChanges`'inde); API `YoklamaSmsBildirimService` (`:62-209`), `BildirimService.Olustur` (`XACT_ABORT ON`). API `KuyrukTetikleyici`'ye erişemez → işleyici periyodu (≤10 sn) beklenir.
- **İşleyici** `GonderimKuyruguIsleyici` (yalnız MVC): döngü 10 sn veya tetik (`:23,59-73`); tur (`:76-115`): 1 dk'da bir geçiş güvenlik ağı (`:83-84`), süresi geçenler SuresiDoldu (`:87-91`), en çok 20×50 satır (`:95-102`), saatte bir 30 gün temizliği (`:105-114`). Üstlenme `UPDATE TOP(n) … WITH (ROWLOCK, READPAST)`, kilidi düşmüş `Durum=1` satırlar da yeniden alınır (`:29-36`). Gönderim: SMS 5 paralel (`Sms/SmsAyarlari.cs:14`), 30 sn timeout (`SmsAltyapiExtensions.cs:19`); push sıralı (`:138-230`); sonuç `SonucuYazAsync` (`:232-247`).
- **Riskler:**
  - En kötü parti süresi 50/5×30 sn = 300 sn = kilit süresi → çoklu örnekte aynı satır yeniden alınıp **çift SMS** (`:36`). Üretimde çoklu örnek olup olmadığı **doğrulanamadı**.
  - SMS gittikten sonra geçmiş/sonuç yazılmadan hata/iptal → satır `Durum=1` kalır, 5 dk sonra yeniden gönderilir (`:63-65,222-225`).
  - Zaman aşımı geçici hata sayılır (`Sms/SmsService.cs:62-67`); sağlayıcı idempotency desteği **doğrulanamadı**.
  - Push en az bir cihaza gittiyse başarılı; token yoksa tamamlanmış sayılır (`:208-212`).
  - Tüm taramalar yalnız **bugün** → gün içinde kuyruğa alınamayanlar kalıcı SMS'siz.

### 7.2 Cihaz senkron outbox — `CihazSenkronKuyrugu`
- Model `MVC/Models/CihazSenkronKuyruguModel.cs:11-31`; unique bekleyen (CihazId, OgrenciId) filtre `Durum=0` (`AppDbContext.cs:599-602`).
- Ekleme `CihazSenkronService.KuyrugaEkleAsync` (`:24-34`, `INSERT … WHERE NOT EXISTS (UPDLOCK, HOLDLOCK)`), `CihaziEsitleAsync` (`:38-53`).
- İşleyici `CihazSenkronIsleyici`: 30 sn veya tetik, kilit 10 dk, parti 200 (`:24,28-33,80-89`); kurallar `CihazSenkronKurallari.SonrakiAdim` (`:40-53`: kalıcı hata 5 denemede Basarisiz, geçici sınırsız); geri döndürmede birleştirme (`:329-364`).
- Risk: yalnız MVC ekler; API öğrenci yazımları senkrona girmez (§3.7-8).

### 7.3 Arka plan servisleri
MVC `Program.cs:120-126`, API `Api/Program.cs:123`.

| Servis | Görev | Aralık |
|---|---|---|
| `KartOkumaOlayIsleyiciService` | ZKTeco olayı → karar → kayıt → SMS → SignalR | Olay güdümlü |
| `ZkBaglantiIzleyiciHostedService` | `!IsConnected` ise yeniden bağlan | 3 → 60 sn |
| `YemekhanePollingService` | Yemekhane log çekme/yazma, ay sonu `ClearGLog` | 1 dk tur, 13:30+, cihaz başına günde 1 başarılı |
| `GonderimKuyruguIsleyici` | SMS/push gönderimi, güvenlik ağı, temizlik | 10 sn / tetik; tarama 1 dk |
| `CihazSenkronIsleyici` | Öğrenci ↔ ZKTeco kullanıcı senkronu | 30 sn / tetik |
| `RandevuArkaPlanService` | Tamamlanan randevular, hatırlatma | 15 dk |
| `BildirimCihaziTemizlemeService` | Eski FCM / refresh token silme | 1 gün |
| API `YoklamaSmsKuyrukTarayici` | Bugünün yoklama SMS'lerini kuyruğa al | 1 dk |

---

## 8. Riskler — önem sırasıyla

| # | Risk | Etki | Referans |
|---|---|---|---|
| 1 | ZKTeco bağlantı kopması algılanmıyor; abonelik `DisconnectAsync` ile siliniyor ✔ | Ana kapı/yemekhane anlık geçişleri sessizce kaybolur | `ZKTecoService.cs:120-145,158-196` |
| 2 | Anlık bağlantı tüm okullarda yalnız ilk ZKTeco cihazına ✔; ana kapı için log telafisi yok | Diğer cihaz/okulların geçişleri hiç kaydedilmez | `ZKTecoService.cs:53-71` |
| 3 | `OnAttTransactionEx` EnrollNumber (=OgrenciId) kart no olarak aranıyor ✔ | Tanımsız düşme veya yanlış öğrenciye kayıt | `ZKTecoService.cs:214`; `KartOkuService.cs:27`; `CihazSenkronIsleyici.cs:242,290` |
| 4 | Sınıf yoklaması 0 ile ezilebilir; sunucu `DurumId` doğrulamaz ✔ | Yoklama verisi kaybı | `OgrenciListeGorunumModel.cs:56,79,93-107`; `YoklamaKayitOgesiDto.cs:7` |
| 5 | Yoklama MERGE'leri HOLDLOCK'suz, unique yok ✔ | Çift satır, çift SMS | Api `OgrenciService.cs:326-339`; `ServisService.cs:198-211` |
| 6 | Yemekhane polling ChangeTracker sızıntısı ✔ | Çift kayıt / art arda hata | `YemekhanePollingService.cs:199-203,360-369` |
| 7 | API'den eklenen/güncellenen kart cihaza senkronlanmaz ✔ | Öğrenci cihazda tanımsız | Api `OgrenciService.cs:493,535` |
| 8 | Yemekhane anlık yol ↔ polling birbirini kilitlemiyor | Çift kayıt, çift SMS | `GecisService.cs:49-82`; `YemekhanePollingService.cs:331-371` |
| 9 | Outbox kilit süresi = en kötü gönderim süresi; gönderim sonrası çökme | Çift SMS | `GonderimKuyruguIsleyici.cs:29-36,222-225`; `GonderimKuyruguKurallari.cs:19-20` |
| 10 | Tüm SMS taramaları yalnız bugün | Gün içinde kuyruğa alınamayan / polling'le geç gelen geçişin SMS'i hiç gitmez | `GonderimKuyruguService.cs:39-40`; `YemekhanePollingService.cs:211-216`; `YoklamaSmsBildirimService.cs:211-215` |
| 11 | Ay sonu `ClearGLog` ayrı bağlantıda, `EnableDevice(false)` yok; saat senkronu yok | Okunmamış log silinebilir | `YemekhanePollingService.cs:383,390,283` |
| 12 | Ay geçişinde otomatik senkron yok (hak tanımı tek yere toplandı) | Ayın 1'inde "Cihazla Eşitle" basılmazsa yeni ayın hakkı cihaza geçmez | `YemekhaneController.cs:136`; `YemekHakki.cs:13` |

**Doğrulanamayanlar (çalışma zamanı/ortam gerektirir):** cihazın hangi olayları gönderdiği (`OnHIDNum` her okutmada mı), STA olmadan COM olaylarının güvenilirliği, üretimde tek/çoklu MVC worker (overlapped recycle), DB ve API sunucularının saat dilimi, yemekhane cihazının anlık olay kanalına bağlı olup olmadığı, SMS sağlayıcısının idempotency desteği, FCM zaman aşımı.
