"use strict";

/* =================== Global State =================== */
let connection;
let ogrenciBilgisiContainer;
let displayTimeoutId = null;
let retryCount = 0;

const config = {
    displayDuration: 1000,       // ms
    maxRetryAttempts: 5,
    initialRetryDelay: 1000,     // ms
    maxRetryDelay: 30000         // ms
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

    ["pointerdown", "keydown", "touchstart"].forEach(ev =>
        document.addEventListener(ev, resumeAudio, { once: true, passive: true })
    );

    if (typeof window.focusKartInput === "function") window.focusKartInput();

    window.addEventListener("offline", () => showTransientBanner("Bağlantı yok (offline).", "#fde68a"));
    window.addEventListener("online", () => showTransientBanner("Bağlantı geri geldi.", "#bbf7d0"));
});

/* =================== Helpers =================== */
function sanitizeHtml(unsafe) {
    if (unsafe === null || unsafe === undefined) return "-";
    return unsafe.toString()
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
    } catch { /* ignore */ }
    return "";
}

/* --- GUID yardımcıları --- */
function normGuid(s) { return (s || "").toString().trim().toLowerCase().replace(/[{}]/g, ""); }
function sameGuid(a, b) { return normGuid(a) === normGuid(b); }

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
    } catch { /* ignore */ }
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
    } catch { /* ignore */ }
}

/* -------- ENUM -> öğle izni -------- */
function lunchAllowedFromEnum(o) {
    if (!o || typeof o !== "object")
        return { allowed: false, source: "default", raw: undefined };

    if (Object.prototype.hasOwnProperty.call(o, "oglenCikisDurumu")) {
        const v = o.oglenCikisDurumu;
        if (typeof v === "number") return { allowed: v === 0, source: "camel-num", raw: v };
        if (typeof v === "string") {
            const s = v.trim().toLowerCase();
            if (s === "0" || s === "evet") return { allowed: true, source: "camel-str", raw: v };
            if (s === "1" || s === "hayır" || s === "hayir") return { allowed: false, source: "camel-str", raw: v };
        }
    }
    if (Object.prototype.hasOwnProperty.call(o, "OglenCikisDurumu")) {
        const v = o.OglenCikisDurumu;
        if (typeof v === "number") return { allowed: v === 0, source: "pascal-num", raw: v };
        if (typeof v === "string") {
            const s = v.trim().toLowerCase();
            if (s === "0" || s === "evet") return { allowed: true, source: "pascal-str", raw: v };
            if (s === "1" || s === "hayır" || s === "hayir") return { allowed: false, source: "pascal-str", raw: v };
        }
    }
    return { allowed: false, source: "default", raw: undefined };
}

/* -------- Banner spec -------- */
function getReasonSpec(reason, ctx) {
    const infoMsg = (ctx && (ctx.Info || ctx.info)) || null;
    const errMsg = (ctx && (ctx.Error || ctx.error)) || null;
    switch ((reason || "").toString()) {
        case "YEMEKHANE_OK": return { fullBg: "#22c55e", text: infoMsg || "YEMEKHANE GEÇİŞİ ONAYLANDI.", beep: false };
        case "YEMEKHANE_RED": return { fullBg: "#dc2626", text: errMsg || "ÖĞRENCİNİN YEMEKHANE GEÇİŞ İZNİ YOK!", beep: true };
        case "YEMEKHANE_LIMIT": return { fullBg: "#dc2626", text: "Bugün için yemekhane geçiş hakkı kullanıldı!", beep: true };
        case "ANA_KAPI_OGLE_OK": return { fullBg: "#22c55e", text: infoMsg || "ÖĞLE ÇIKIŞI ONAYLANDI.", beep: false };
        case "ANA_KAPI_OGLE_RED": return { fullBg: "#dc2626", text: errMsg || "ÖĞRENCİNİN ÖĞLE ÇIKIŞ İZNİ YOK!", beep: true };
        case "ANA_KAPI_OGLE_LIMIT": return { fullBg: "#dc2626", text: "Bugün için öğle çıkış hakkı kullanıldı!", beep: true };
        case "ANA_KAPI_OGLE_DONUS_LIMIT": return { fullBg: "#dc2626", text: "Bugün öğle çıkış ve dönüş zaten yapılmış!", beep: true };
        default: return { fullBg: "#e5e7eb", text: "GEÇİŞ BİLGİSİ", beep: false };
    }
}

/* -------- Öğrenci kartı renderer -------- */
function renderStudentFragment(o, lunch) {
    const reason = (o.reason || o.Reason || "").toString();
    const istasyonRaw = (o.istasyon || o.Istasyon || "").toString();
    const istasyon = istasyonRaw.toLowerCase();
    const isYemekhane = istasyon === "yemekhane";

    const container = document.createElement("div");
    container.className = "row g-0 mx-auto";
    container.style.maxWidth = "900px";
    container.style.borderRadius = "12px";
    container.style.border = "2px solid #e5e7eb";
    container.style.overflow = "hidden";

    const colImg = document.createElement("div");
    colImg.className = "col-md-5 d-flex align-items-center justify-content-center p-3";
    colImg.style.borderRadius = "12px 0 0 12px";
    colImg.style.background = "transparent";

    const imgUrl = sanitizeUrl(o.ogrenciGorsel || "");
    if (imgUrl) {
        const img = document.createElement("img");
        img.src = imgUrl;
        img.alt = "Öğrenci Fotoğrafı";
        img.style.width = "340px";
        img.style.height = "450px";
        img.style.objectFit = "cover";
        img.style.borderRadius = "8px";
        img.style.maxWidth = "100%";
        colImg.appendChild(img);
    } else {
        const noImg = document.createElement("h5");
        noImg.textContent = "Görsel Yok";
        noImg.style.color = "#111";
        colImg.appendChild(noImg);
    }

    const colInfo = document.createElement("div");
    colInfo.className = "col-md-7";
    colInfo.style.borderRadius = "0 12px 12px 0";
    colInfo.style.background = "#ffffff";
    colInfo.style.color = "#111";

    const body = document.createElement("div");
    body.className = "card-body p-4 d-flex flex-column justify-content-start";
    body.style.fontFamily = "'Segoe UI', Arial, sans-serif";

    const header = document.createElement("div");
    header.className = "d-flex align-items-center justify-content-between mb-3";

    const hName = document.createElement("h3");
    hName.className = "card-subtitle mb-0";
    hName.style.color = "#111";
    hName.style.fontSize = "2.0rem";
    hName.style.fontWeight = "700";
    hName.textContent = sanitizeHtml(o.ogrenciAdSoyad || "-");

    const badge = document.createElement("span");
    badge.className = "badge";
    badge.style.fontSize = "1rem";
    badge.style.padding = "0.5rem 0.75rem";
    badge.style.borderRadius = "9999px";
    badge.style.border = "1px solid #e5e7eb";
    badge.style.background = "#ffffff";
    badge.style.color = "#16a34a";

    const reasonStr = (o.reason || o.Reason || "").toString();
    const isReject =
        reasonStr === "YEMEKHANE_RED" ||
        reasonStr === "YEMEKHANE_LIMIT" ||
        reasonStr === "ANA_KAPI_OGLE_RED" ||
        reasonStr === "ANA_KAPI_OGLE_LIMIT" ||
        reasonStr === "ANA_KAPI_OGLE_DONUS_LIMIT" ||
        (o.gecisTipi || o.GecisTipi || "").toString().toLowerCase() === "reddedildi";

    if (isYemekhane) {
        // YEMEKHANE için badge metni
        if (reasonStr === "YEMEKHANE_OK") {
            badge.textContent = "Yemekhane: İzin Var";
            badge.style.color = "#16a34a";
        } else if (reasonStr === "YEMEKHANE_RED") {
            badge.textContent = "Yemekhane: İzin Yok!";
            badge.style.color = "#dc2626";
        } else if (reasonStr === "YEMEKHANE_LIMIT") {
            badge.textContent = "Yemekhane: Günlük Hak Kullanıldı!";
            badge.style.color = "#dc2626";
        } else {
            badge.textContent = "Yemekhane";
            badge.style.color = isReject ? "#dc2626" : "#16a34a";
        }
    } else {
        // ANA KAPI için badge metni
        if (reasonStr === "ANA_KAPI_OGLE_OK") {
            badge.textContent = "Öğle Çıkış: Onaylandı";
            badge.style.color = "#16a34a";
        } else if (reasonStr === "ANA_KAPI_OGLE_RED") {
            badge.textContent = "Öğle Çıkış: İzin Yok!";
            badge.style.color = "#dc2626";
        } else if (reasonStr === "ANA_KAPI_OGLE_LIMIT" || reasonStr === "ANA_KAPI_OGLE_DONUS_LIMIT") {
            badge.textContent = "Öğle Çıkış/Dönüş: Günlük Hak Kullanıldı!";
            badge.style.color = "#dc2626";
        } else {
            const lunch = lunchAllowedFromEnum(o); // sadece badge rengi/varsayılan metin için
            badge.textContent = lunch.allowed ? "Öğle Çıkış: İzin Var" : "Öğle Çıkış: İzin Yok";
            badge.style.color = lunch.allowed ? "#16a34a" : "#dc2626";
        }
    }

    header.append(hName, badge);
    body.appendChild(header);

    const fields = [
        ["Öğrenci No:", o.ogrenciNo],
        ["Sınıf:", o.ogrenciSinif],
        ["Giriş Zamanı:", o.ogrenciGirisSaati],
        ["Çıkış Zamanı:", o.ogrenciCikisSaati]
    ];

    fields.forEach(([label, value]) => {
        const p = document.createElement("p");
        p.className = "fs-5 text-start";
        p.style.display = "flex";
        p.style.alignItems = "center";
        p.style.marginBottom = "1.0rem";
        p.style.lineHeight = "1.7";
        p.style.color = "#111";
        p.innerHTML = `
            <span style="min-width: 150px; font-weight: 600;">${label}</span>
            <span style="
                flex: 1;
                border-bottom: 1px solid #cbd5e1;
                padding-bottom: 4px;
                overflow: hidden;
                text-overflow: ellipsis;
                color:#111;
            ">
                ${sanitizeHtml(value)}
            </span>
        `;
        body.appendChild(p);
    });

    colInfo.appendChild(body);
    container.append(colImg, colInfo);
    return container;
}

/* =================== Main Flow =================== */
function showStudentInfo(o) {
    if (!ogrenciBilgisiContainer) return;
    if (displayTimeoutId) { clearTimeout(displayTimeoutId); displayTimeoutId = null; }

    const pageGuid = (window.CIHAZ_GUID || "").toString();
    const dtoGuid = (o?.cihazKodu || o?.CihazKodu || "").toString();
    if (pageGuid && dtoGuid && typeof sameGuid === "function" && !sameGuid(pageGuid, dtoGuid)) return;

    if (!o || typeof o !== "object") {
        ogrenciBilgisiContainer.replaceChildren(renderErrorFragment("Geçersiz veri alındı."));
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
    if (effectiveReason === "GENEL_OK" && ANA_KAPI_SET.has(istasyon) && gecisTipi === "giriş") {
        bannerText = "ÖĞLE DÖNÜŞÜ ONAYLANDI.";
    }

    const frag = document.createDocumentFragment();

    const banner = document.createElement("div");
    banner.className = "alert fw-bold fs-5 text-center mb-3";
    banner.style.background = spec.fullBg || "#e5e7eb";
    banner.style.border = "none";
    banner.style.color = "#111";
    banner.textContent = bannerText;
    frag.appendChild(banner);

    if (spec.beep) playBeep();

    frag.appendChild(renderStudentFragment(o, lunchAllowedFromEnum(o)));

    ogrenciBilgisiContainer.replaceChildren(frag);

    const formContainer = document.getElementById("ogrenciFormAlani");
    if (formContainer) formContainer.style.display = "none";
    const baslik = document.getElementById("sayfaBasligi");
    if (baslik) baslik.style.display = "none";

    displayTimeoutId = setTimeout(() => {
        ogrenciBilgisiContainer.innerHTML = "";
        if (formContainer) formContainer.style.display = "";
        if (baslik) baslik.style.display = "";
        if (typeof window.focusKartInput === "function") window.focusKartInput();
    }, config.displayDuration);
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

    connection.onclose(err => {
        if (err) {
            console.error("SignalR bağlantısı kapandı:", err);
            showTransientBanner("Bağlantı kapandı.", "#fecaca");
        }
    });
}

function startConnection() {
    connection.start()
        .then(() => {
            console.log("SignalR bağlantısı kuruldu.");
        })
        .catch(err => {
            console.error("SignalR bağlantısı başlatılamadı:", err);
            retryCount++;
            if (retryCount <= config.maxRetryAttempts) {
                const retryDelay = Math.min(
                    config.initialRetryDelay * Math.pow(2, retryCount),
                    config.maxRetryDelay
                );
                console.log(`Yeniden denenecek (${retryCount}. deneme) ${retryDelay}ms sonra...`);
                setTimeout(startConnection, retryDelay);
            } else {
                console.error(`Maksimum yeniden deneme sayısına ulaşıldı (${config.maxRetryAttempts}).`);
                showTransientBanner("Bağlantı kurulamadı.", "#fecaca", 2500);
            }
        });
}

/* =================== Cleanup =================== */
window.addEventListener("beforeunload", () => {
    if (displayTimeoutId) clearTimeout(displayTimeoutId);
    if (transientBannerId) clearTimeout(transientBannerId);
    if (connection) {
        connection.off("OgrenciBilgisiAl");
        try { connection.stop(); } catch { /* ignore */ }
    }
});