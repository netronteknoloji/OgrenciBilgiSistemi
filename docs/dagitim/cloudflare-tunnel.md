# Cloudflare Tunnel ile HTTPS'e geçiş (SEC-06)

Amaç: Mobil uygulama ve panel, IP adresi ve açık port yerine `https://` alan adlarıyla çalışsın.
- Sertifika Cloudflare'de, otomatik ve süresiz yenilenir.
- Modemde port açmak gerekmez.
- Sunucunun IP'si değişse de uygulamalar etkilenmez.

```
Mobil / tarayıcı ──HTTPS──► Cloudflare ──(tünel, sunucu dışarı bağlanır)──► cloudflared ──HTTP──► IIS
```

IIS sitelerinin ayarları değişmez; IIS içeride HTTP'de çalışmaya devam eder.

Örnek adlar (istediğiniz gibi değiştirebilirsiniz):

| Ad | Hedef |
|---|---|
| `api.netronyazilim.com` | API sitesi (bugün `81.214.75.22:5196`) |
| `panel.netronyazilim.com` | Web paneli (MVC) |

---

## 0. Hazırlık

- **Alan adı paneline erişim.** `netronyazilim.com`'u satın aldığınız firmanın paneline girebiliyor olmanız gerekir.
- **Web sitesi ve e-posta riski.** Alan adının DNS'i Cloudflare'e taşınacak. Mevcut kayıtlar (web sitesi, e-posta/MX) Cloudflare'e kopyalanır ama **1. adımda listeyi mutlaka karşılaştırın**. Eksik bir MX kaydı e-postayı durdurur.
  - Bu riski hiç almak istemezseniz bu iş için ayrı, yeni bir alan adı alın (örn. `netronokul.com`) ve aşağıdaki adımları onunla yapın.
- **Sunucuya yönetici erişimi.** IIS'in çalıştığı Windows sunucuda yönetici olarak oturum açabilmelisiniz.
- **IIS portlarını not edin.** IIS Yöneticisi → Siteler → her site → *Bağlamalar* (Bindings) ekranına bakın.
  - API sitesi: `http`, port **5196**.
  - Panel sitesi: `http`, port **_____** (siz doldurun).
  - Bağlamada bir **ana bilgisayar adı (host name)** yazılıysa onu da not edin (bkz. 3. adımdaki not).

## 1. Alan adını Cloudflare'e ekleyin (ücretsiz)

1. https://dash.cloudflare.com adresinde hesap açın.
2. **Add a domain** → `netronyazilim.com` → **Free** planı seçin.
3. Cloudflare mevcut DNS kayıtlarını tarar ve listeler. Alan adı firmanızın panelindeki DNS kayıtlarıyla **tek tek karşılaştırın**:
   - `www`, `@` (web sitesi)
   - `MX`, `mail`, `TXT`/SPF (e-posta)
   - Eksik olanı elle ekleyin.
4. **E-posta kayıtları gri bulutta kalmalı.** `mail`, MX ve FTP gibi kayıtlarda turuncu bulutu kapatın ("DNS only").
5. Cloudflare iki **ad sunucusu (nameserver)** verir (örn. `xxx.ns.cloudflare.com`). Alan adı firmanızın panelinde nameserver'ları bu ikisiyle değiştirin.
6. Etkinleşmesi birkaç dakika ile 24 saat sürebilir. Cloudflare "Active" diye e-posta gönderir.

## 2. Cloudflare güvenlik ayarları

Cloudflare panelinde, alan adınızı seçtikten sonra:

| Menü | Ayar | Değer | Neden |
|---|---|---|---|
| SSL/TLS → Overview | Şifreleme modu | **Full** | Web sitenizin barındırma firmasındaki sertifikası alan adına uymuyor. "Full (strict)" seçilirse site açılmaz. |
| SSL/TLS → Edge Certificates | Always Use HTTPS | **Açık** | `http://` ile gelen her istek `https://`'e yönlenir. |
| SSL/TLS → Edge Certificates | Minimum TLS Version | **TLS 1.2** | |
| Security → Bots | Bot Fight Mode | **Kapalı** | Açıkken mobil uygulamanın isteklerini bot sanıp engelleyebilir. |
| Network | WebSockets | **Açık** (varsayılan) | Paneldeki canlı kart okuma ekranı (SignalR) bunu kullanır. |

> Bu adımdan sonra `https://www.netronyazilim.com/okullar.json` da geçerli bir sertifikayla açılır. Yeterli koşul: `www` kaydının turuncu bulutta olması.

## 3. Tüneli oluşturun

1. Cloudflare panelinde **Zero Trust** → **Networks** → **Tunnels** → **Create a tunnel** → **Cloudflared**.
   - Zero Trust ilk açılışta bir ekip adı ve ücretsiz plan seçmenizi ister.
2. Tünele bir ad verin (örn. `okul-sunucu`) → **Save**.
3. **Windows** seçin. Ekranda iki komut görünür:
   - Kurulum dosyası indirme bağlantısı
   - `cloudflared.exe service install <uzun-token>`
4. Sunucuda:
   - Kurulum dosyasını (`cloudflared-windows-amd64.msi`) indirip kurun.
   - **Yönetici olarak** açılmış PowerShell'de `cloudflared.exe service install <uzun-token>` komutunu çalıştırın.
   - Bu komut tüneli bir Windows hizmeti olarak kurar; sunucu yeniden başlasa da kendiliğinden açılır.
   - **Token gizlidir.** Kimseyle paylaşmayın, hiçbir dosyaya veya git'e yazmayın.
5. Cloudflare ekranında tünel **Healthy** (yeşil) görünmeli → **Next**.
6. **Public Hostnames** bölümüne iki kayıt ekleyin:

   | Subdomain | Domain | Type | URL |
   |---|---|---|---|
   | `api` | `netronyazilim.com` | HTTP | `localhost:5196` |
   | `panel` | `netronyazilim.com` | HTTP | `localhost:<panel portu>` |

   > IIS bağlamasında bir ana bilgisayar adı (host name) yazılıysa:
   > - Kaydın **Additional application settings → HTTP Settings → HTTP Host Header** alanına o adı yazın.
   > - Bağlamada ad yoksa bu alanı boş bırakın.

## 4. Kontrol

Sunucu dışından (örneğin cep telefonu hattına bağlı bir bilgisayardan):

```powershell
curl.exe -s -o NUL -w "%{http_code}`n" https://api.netronyazilim.com/api/uygulama-versiyon   # 200 beklenir
curl.exe -s -o NUL -w "%{http_code}`n" https://www.netronyazilim.com/okullar.json            # 200 beklenir
```

Ardından tarayıcıda `https://panel.netronyazilim.com` adresini açın:
- Giriş yapın.
- Kart okuma ekranında bir kart okutun; ekrana düşmeli.

Sonucu bana iletin; sonraki adımlara bu kontrollerden sonra geçilir.

## 5. Geçiş (kontroller başarılıysa)

Bu sıra eski uygulama sürümlerinin bozulmaması için önemlidir.

1. **`okullar.json`'daki API adresini değiştirin.** `http://81.214.75.22:5196/api/` yerine `https://api.netronyazilim.com/api/` yazın.
   - Eski sürümler de HTTPS adrese bağlanabildiği için, bu değişiklikten sonra okul seçen veya yeniden giriş yapan her kullanıcı şifreli bağlantıya geçer.
2. **Benim kod tarafım (siz "devam" deyince):**
   - Mobil uygulamada adresleri `https://` yapıp Android ve iOS'ta düz HTTP iznini kapatacağım.
   - API'nin, Cloudflare'in ilettiği gerçek kullanıcı IP'sini ve HTTPS bilgisini doğru okumasını sağlayacağım.
   - Ardından yeni mobil sürüm çıkacak (yayın sizden).
3. **Eski bağlantı noktası bir süre açık kalmalı.** Oturumu açık olan eski sürümler kayıtlı eski adresi kullanmaya devam eder. Bu yüzden 5196 portu yeni sürüm yaygınlaşana kadar açık kalmalı.
4. **Son adım.** Zorunlu güncelleme ile eski sürüm kapatıldıktan sonra:
   - Modemdeki 5196 port yönlendirmesini kaldırın.
   - Sunucu artık internete hiç açık port göstermez.

## Sorun giderme

| Belirti | Olası neden / çözüm |
|---|---|
| Tünel "Down" | Sunucuda `services.msc` → **Cloudflared agent** hizmeti çalışıyor mu? Değilse başlatın. |
| `502 Bad Gateway` | Public Hostname'deki port yanlış ya da IIS sitesi durmuş. Sunucuda `http://localhost:5196` açılıyor mu, kontrol edin. |
| `404` veya IIS varsayılan sayfası | IIS bağlamasında ana bilgisayar adı var; 3. adımdaki **HTTP Host Header** notuna bakın. |
| Web sitesi açılmıyor | SSL modu "Full (strict)" seçilmiş olabilir → **Full** yapın. |
| E-posta gelmiyor | MX kaydı eksik ya da turuncu bulutta. 1. adımdaki kayıtları kontrol edin. |
