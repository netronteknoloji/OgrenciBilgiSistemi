"use strict";

/* =================== Global State =================== */
let connection;
let ogrenciBilgisiContainer;
let displayTimeoutId = null;
let retryCount = 0;

const config = {
  displayDuration: 2000, // ms
  initialRetryDelay: 1000, // ms
  maxRetryDelay: 30000, // ms
};

/* lightweight UI banners (offline/reconnect/info) */
let transientBannerId = null;
function showTransientBanner(text, bg = "#e5e7eb", ms = 1800) {
  const host = ogrenciBilgisiContainer || document.body;
  const id = "transient-banner";
  let div = document.getElementById(id);
  if (!div) {
    div = document.createElement("div");
    div.id = id;
    div.style.position = "fixed";
    div.style.left = "50%";
    div.style.top = "12px";
    div.style.transform = "translateX(-50%)";
    div.style.zIndex = "9999";
    div.style.padding = "10px 14px";
    div.style.borderRadius = "10px";
    div.style.boxShadow = "0 8px 24px rgba(0,0,0,.18)";
    div.style.color = "#111";
    div.style.fontWeight = "600";
    div.style.fontFamily = "'Segoe UI', Arial, sans-serif";
    div.style.maxWidth = "90vw";
    div.style.textAlign = "center";
    document.body.appendChild(div);
  }
  div.textContent = text || "";
  div.style.background = bg;
  div.style.display = "block";

  if (transientBannerId) clearTimeout(transientBannerId);
  transientBannerId = setTimeout(() => {
    div.style.display = "none";
  }, ms);
}

/* =================== Boot =================== */
document.addEventListener("DOMContentLoaded", () => {
  if (!(window.signalR && window.signalR.HubConnectionBuilder)) {
    console.warn("[signalr-handler] signalR bulunamadı.");
    return;
  }

  ogrenciBilgisiContainer = document.getElementById("ogrenciBilgisi");
  if (!ogrenciBilgisiContainer) {
    console.warn("[signalr-handler] #ogrenciBilgisi yok.");
    return;
  }

  ogrenciBilgisiContainer.setAttribute("role", "status");
  ogrenciBilgisiContainer.setAttribute("aria-live", "polite");

  connection = new signalR.HubConnectionBuilder()
    .withUrl("/kartOkuHub")
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  registerConnectionEvents();
  startConnection();

  ["pointerdown", "keydown", "touchstart"].forEach((ev) =>
    document.addEventListener(ev, resumeAudio, { once: true, passive: true }),
  );

  if (typeof window.focusKartInput === "function") window.focusKartInput();

  window.addEventListener("offline", () =>
    showTransientBanner("Bağlantı yok (offline).", "#fde68a"),
  );
  // Ağ geri gelince beklemeden bağlanmayı dene
  window.addEventListener("online", () => {
    showTransientBanner("Bağlantı geri geldi.", "#bbf7d0");
    if (yenidenBaglanmaZamanlayici) {
      clearTimeout(yenidenBaglanmaZamanlayici);
      yenidenBaglanmaZamanlayici = null;
    }
    retryCount = 0;
    startConnection();
  });
});

/* =================== Helpers =================== */
function sanitizeHtml(unsafe) {
  if (unsafe === null || unsafe === undefined) return "-";
  return unsafe
    .toString()
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}

function sanitizeUrl(url) {
  try {
    const u = new URL(url, document.baseURI);
    if (["http:", "https:"].includes(u.protocol)) return u.toString();
  } catch {
    /* ignore */
  }
  return "";
}

/* --- GUID yardımcıları --- */
function normGuid(s) {
  return (s || "").toString().trim().toLowerCase().replace(/[{}]/g, "");
}
function sameGuid(a, b) {
  return normGuid(a) === normGuid(b);
}

/* -------- Ses -------- */
let audioCtx = null;
function getAudioCtx() {
  if (!audioCtx) {
    const Ctx = window.AudioContext || window.webkitAudioContext;
    if (!Ctx) return null;
    audioCtx = new Ctx();
  }
  return audioCtx;
}
function resumeAudio() {
  try {
    const ctx = getAudioCtx();
    if (ctx && ctx.state === "suspended") ctx.resume();
  } catch {
    /* ignore */
  }
}
function playBeep(duration = 180, frequency = 880, volume = 0.4) {
  try {
    const ctx = getAudioCtx();
    if (!ctx) return;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    gain.gain.value = volume;
    osc.type = "square";
    osc.frequency.setValueAtTime(frequency, ctx.currentTime);
    osc.connect(gain);
    gain.connect(ctx.destination);
    const now = ctx.currentTime;
    osc.start(now);
    osc.stop(now + duration / 1000);
  } catch {
    /* ignore */
  }
}

/* -------- Banner spec -------- */
function getReasonSpec(reason, ctx) {
  const infoMsg = (ctx && (ctx.Info || ctx.info)) || null;
  const errMsg = (ctx && (ctx.Error || ctx.error)) || null;
  switch ((reason || "").toString()) {
    case "YEMEKHANE_OK":
      return {
        fullBg: "#16a34a",
        text: infoMsg || "YEMEKHANE GEÇİŞİ ONAYLANDI.",
        beep: false,
      };
    case "YEMEKHANE_RED":
      return {
        fullBg: "#dc2626",
        text: errMsg || "ÖĞRENCİNİN YEMEKHANE GEÇİŞ İZNİ YOK!",
        beep: true,
      };
    case "YEMEKHANE_LIMIT":
      return {
        fullBg: "#dc2626",
        text: "Bugün için yemekhane geçiş hakkı kullanıldı!",
        beep: true,
      };
    case "ANA_KAPI_OGLE_OK":
      return {
        fullBg: "#16a34a",
        text: infoMsg || "ÖĞLE ÇIKIŞI ONAYLANDI.",
        beep: false,
      };
    case "ANA_KAPI_OGLE_DONUS_OK":
      return {
        fullBg: "#16a34a",
        text: infoMsg || "ÖĞLE DÖNÜŞÜ ONAYLANDI.",
        beep: false,
      };
    case "ANA_KAPI_OGLE_CIKIS_YAPILDI":
      return {
        fullBg: "#dc2626",
        text: errMsg || "Öğle çıkışı yapıldı!",
        beep: true,
      };
    case "ANA_KAPI_OGLE_SAAT_DISI":
      return {
        fullBg: "#dc2626",
        text: errMsg || "ÖĞLE ÇIKIŞ SAATİ DEĞİL!",
        beep: true,
      };
    case "KART_TANIMSIZ":
      return {
        fullBg: "#dc2626",
        text: errMsg || "KART TANIMSIZ!",
        beep: true,
      };
    case "ANA_KAPI_OGLE_RED":
      return {
        fullBg: "#dc2626",
        text: errMsg || "ÖĞRENCİNİN ÖĞLE ÇIKIŞ İZNİ YOK!",
        beep: true,
      };
    case "ANA_KAPI_OGLE_LIMIT":
      return {
        fullBg: "#dc2626",
        text: "Bugün için öğle çıkış hakkı kullanıldı!",
        beep: true,
      };
    case "ANA_KAPI_OGLE_DONUS_LIMIT":
      return {
        fullBg: "#dc2626",
        text: "Bugün öğle çıkış ve dönüş yapıldı!",
        beep: true,
      };
    default:
      return { fullBg: "#e5e7eb", text: "GEÇİŞ BİLGİSİ", beep: false };
  }
}

/* -------- Tam ekran sonuç renderer -------- */
// Değerler yalnızca textContent ile basılır (XSS); "-" olan alanlar gösterilmez
function bosMu(v) {
  return v === null || v === undefined || v.toString().trim() === "" || v.toString().trim() === "-";
}

function el(tag, className, text) {
  const d = document.createElement(tag);
  if (className) d.className = className;
  if (text !== undefined) d.textContent = text;
  return d;
}

function renderSonucOverlay(o, spec, bannerText, ogrenciGoster) {
  const overlay = el("div", "kart-sonuc-overlay");
  overlay.style.background = spec.fullBg || "#e5e7eb";
  // Gri (bilgi) zeminde koyu, renkli zeminde beyaz yazı
  overlay.style.color = spec.fullBg === "#e5e7eb" || !spec.fullBg ? "#111" : "#fff";

  overlay.appendChild(el("div", "kart-sonuc-durum", bannerText));

  if (ogrenciGoster) {
    const imgUrl = sanitizeUrl(o.ogrenciGorsel || o.OgrenciGorsel || "");
    if (imgUrl) {
      const img = el("img", "kart-sonuc-foto");
      img.src = imgUrl;
      img.alt = "Öğrenci Fotoğrafı";
      overlay.appendChild(img);
    } else {
      overlay.appendChild(el("div", "kart-sonuc-foto kart-sonuc-foto-yok", "Görsel Yok"));
    }

    overlay.appendChild(el("div", "kart-sonuc-ad", (o.ogrenciAdSoyad || o.OgrenciAdSoyad || "-").toString()));

    const istasyon = (o.istasyon || o.Istasyon || "").toString().toLowerCase();
    if (istasyon !== "yemekhane") {
      const cikis = o.ogrenciCikisSaati || o.OgrenciCikisSaati;
      const giris = o.ogrenciGirisSaati || o.OgrenciGirisSaati;
      if (!bosMu(cikis)) overlay.appendChild(el("div", "kart-sonuc-saat", `Çıkış: ${cikis}`));
      if (!bosMu(giris)) overlay.appendChild(el("div", "kart-sonuc-saat", `Dönüş: ${giris}`));
    }
  }

  return overlay;
}

/* =================== Main Flow =================== */
function scheduleEkranTemizle() {
  displayTimeoutId = setTimeout(() => {
    if (ogrenciBilgisiContainer) ogrenciBilgisiContainer.innerHTML = "";
    const formContainer = document.getElementById("ogrenciFormAlani");
    const baslik = document.getElementById("sayfaBasligi");
    if (formContainer) formContainer.style.display = "";
    if (baslik) baslik.style.display = "";
    if (typeof window.focusKartInput === "function") window.focusKartInput();
    displayTimeoutId = null;
    console.debug("[kart] ekran temizlendi");
  }, config.displayDuration);
}

function showStudentInfo(o) {
  if (!ogrenciBilgisiContainer) return;
  if (displayTimeoutId) {
    clearTimeout(displayTimeoutId);
    displayTimeoutId = null;
  }

  const pageGuid = (window.CIHAZ_GUID || "").toString();
  const dtoGuid = (o?.cihazKodu || o?.CihazKodu || "").toString();
  if (
    pageGuid &&
    dtoGuid &&
    typeof sameGuid === "function" &&
    !sameGuid(pageGuid, dtoGuid)
  ) {
    // Yabancı cihazın mesajı: kendi ekranımıza dokunma; ama bizim mevcut içerik
    // gösterimde ise onun temizliğini yine garanti altına al.
    if (
      ogrenciBilgisiContainer &&
      ogrenciBilgisiContainer.childElementCount > 0
    ) {
      scheduleEkranTemizle();
    }
    return;
  }

  if (!o || typeof o !== "object") {
    ogrenciBilgisiContainer.replaceChildren(
      renderErrorFragment("Geçersiz veri alındı."),
    );
    scheduleEkranTemizle();
    return;
  }

  const istasyonRaw = (o.istasyon || o.Istasyon || "").toString();
  const istasyon = istasyonRaw.toLowerCase();
  const reasonRaw = (o.reason || o.Reason || "").toString();
  const gecisTipi = (o.gecisTipi || o.GecisTipi || "").toString().toLowerCase();

  // Sadece sunucudan gelen Reason'a güven; yoksa güvenli varsayılan kullan
  const effectiveReason = (() => {
    if (reasonRaw) return reasonRaw;
    if (gecisTipi === "reddedildi") {
      if (istasyon === "yemekhane") return "YEMEKHANE_RED";
      const ANA_KAPI_SET = new Set(["anakapi", "ana kapi", "ana kapı"]);
      if (ANA_KAPI_SET.has(istasyon)) return "ANA_KAPI_OGLE_RED";
    }
    return "GENEL_OK";
  })();

  const spec = getReasonSpec(effectiveReason, o);

  // GENEL_OK + Ana Kapı + Giriş ise (öğle dönüş), yeni Reason üretmeden yalnızca metni iyileştir
  let bannerText = spec.text || "GEÇİŞ BİLGİSİ";
  const ANA_KAPI_SET = new Set(["anakapi", "ana kapi", "ana kapı"]);
  if (
    effectiveReason === "GENEL_OK" &&
    ANA_KAPI_SET.has(istasyon) &&
    gecisTipi === "giriş"
  ) {
    bannerText = "ÖĞLE DÖNÜŞÜ ONAYLANDI.";
  }

  if (spec.beep) playBeep();

  // Tanımsız kartta gösterilecek öğrenci yok; sadece durum metni.
  // Form gizlenmez: overlay üstünü kaplar, input odakta kalır ve sıradaki kart okunabilir.
  ogrenciBilgisiContainer.replaceChildren(
    renderSonucOverlay(o, spec, bannerText, effectiveReason !== "KART_TANIMSIZ"),
  );

  scheduleEkranTemizle();
}

/* -------- Basit hata fragment'i -------- */
function renderErrorFragment(msg) {
  const d = document.createElement("div");
  d.className = "alert alert-danger";
  d.textContent = msg || "Bilinmeyen hata";
  return d;
}

/* =================== SignalR =================== */
function registerConnectionEvents() {
  connection.on("OgrenciBilgisiAl", showStudentInfo);

  connection.onreconnecting(() => {
    showTransientBanner("Bağlantı koptu, yeniden bağlanılıyor…", "#fde68a");
    console.warn("SignalR yeniden bağlanıyor...");
  });

  connection.onreconnected(() => {
    showTransientBanner("SignalR tekrar bağlandı.", "#bbf7d0");
    retryCount = 0;
    console.log("SignalR tekrar bağlandı.");
  });

  // Otomatik yeniden bağlanma vazgeçerse (ör. uzun sunucu kesintisi) bağlantı burada kapanır.
  // Kiosk gün boyu açık kalır: kapanıştan sonra da süresiz yeniden denenir.
  connection.onclose((err) => {
    if (sayfaKapaniyor) return;
    if (err) console.error("SignalR bağlantısı kapandı:", err);
    showTransientBanner("Bağlantı kapandı, yeniden bağlanılıyor…", "#fecaca", 3000);
    retryCount = 0;
    yenidenBaglanmayiPlanla();
  });
}

let sayfaKapaniyor = false;
let yenidenBaglanmaZamanlayici = null;

function yenidenBaglanmayiPlanla() {
  if (yenidenBaglanmaZamanlayici || sayfaKapaniyor) return;
  retryCount++;
  const retryDelay = Math.min(
    config.initialRetryDelay * Math.pow(2, retryCount),
    config.maxRetryDelay,
  );
  console.log(`Yeniden denenecek (${retryCount}. deneme) ${retryDelay}ms sonra...`);
  yenidenBaglanmaZamanlayici = setTimeout(() => {
    yenidenBaglanmaZamanlayici = null;
    startConnection();
  }, retryDelay);
}

// Oturum düşmüşse (401) yeniden denemek anlamsız: sayfa yenilenir, giriş ekranına yönlenir
function oturumDusmusMu(err) {
  const mesaj = (err && (err.message || err.toString())) || "";
  return (err && err.statusCode === 401) || mesaj.includes("401") || mesaj.includes("Unauthorized");
}

function startConnection() {
  if (sayfaKapaniyor || connection.state !== signalR.HubConnectionState.Disconnected) return;
  connection
    .start()
    .then(() => {
      retryCount = 0;
      console.log("SignalR bağlantısı kuruldu.");
      showTransientBanner("Bağlantı kuruldu.", "#bbf7d0");
    })
    .catch((err) => {
      console.error("SignalR bağlantısı başlatılamadı:", err);
      if (oturumDusmusMu(err)) {
        window.location.reload();
        return;
      }
      // Süresiz denenir (en fazla maxRetryDelay aralıkla); ekran uyarıyı görünür tutar
      showTransientBanner("Bağlantı kurulamadı, yeniden deneniyor…", "#fecaca", config.maxRetryDelay);
      yenidenBaglanmayiPlanla();
    });
}

/* =================== Cleanup =================== */
window.addEventListener("beforeunload", () => {
  sayfaKapaniyor = true;
  if (yenidenBaglanmaZamanlayici) clearTimeout(yenidenBaglanmaZamanlayici);
  if (displayTimeoutId) clearTimeout(displayTimeoutId);
  if (transientBannerId) clearTimeout(transientBannerId);
  if (connection) {
    connection.off("OgrenciBilgisiAl");
    try {
      connection.stop();
    } catch {
      /* ignore */
    }
  }
});
