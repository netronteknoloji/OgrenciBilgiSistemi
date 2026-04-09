using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Abstractions;
using OgrenciBilgiSistemi.Infrastructure;
using OgrenciBilgiSistemi.Infrastructure.FileStorage;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Hubs;
using OgrenciBilgiSistemi.Shared.Models;
using OgrenciBilgiSistemi.Shared.Services;
using OgrenciBilgiSistemi.Sms;
using OgrenciBilgiSistemi.Services;
using OgrenciBilgiSistemi.Services.BackgroundServices;
using OgrenciBilgiSistemi.Services.Implementations;
using OgrenciBilgiSistemi.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// --------------------
// Multi-tenant yapılandırma
// --------------------
var okullar = builder.Configuration.GetSection("Okullar").Get<List<OkulBilgiAyari>>();
if (okullar is null || okullar.Count == 0)
    throw new InvalidOperationException("Okullar yapılandırılmamış. appsettings.json içinde Okullar bölümünü ekleyin.");

builder.Services.Configure<List<OkulBilgiAyari>>(builder.Configuration.GetSection("Okullar"));
builder.Services.AddSingleton<OkulYapilandirmaServisi>();
builder.Services.AddScoped<TenantBaglami>();

// --------------------
// DbContext — per-request connection string (pool kullanılamaz)
// --------------------
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var tenantBaglami = serviceProvider.GetRequiredService<TenantBaglami>();
    if (!string.IsNullOrEmpty(tenantBaglami.ConnectionString))
    {
        options.UseSqlServer(tenantBaglami.ConnectionString, sql =>
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null));
    }
    else
    {
        // Fallback: ilk okulun connection string'i (background service'ler için)
        var firstOkul = okullar.FirstOrDefault();
        if (firstOkul is not null && !string.IsNullOrEmpty(firstOkul.ConnectionString))
        {
            options.UseSqlServer(firstOkul.ConnectionString, sql =>
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null));
        }
    }
});

// --------------------
// MVC + Global Authorize + Menu Yetki + Tenant Zorunlu
// --------------------
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AuthorizeFilter());
    o.Filters.Add(typeof(MenuYetkiFilter));
    o.Filters.Add(typeof(TenantZorunluFilter));
});

// --------------------
// Session (Genel Admin okul seçimi için)
// --------------------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// --------------------
// HttpContext erişimi (servis katmanında kullanıcı bilgilerine erişim için)
// --------------------
builder.Services.AddHttpContextAccessor();

// --------------------
// App services
// --------------------
builder.Services.AddScoped<IAidatService, AidatService>();
builder.Services.AddScoped<IGecisService, GecisService>();
builder.Services.AddSingleton<IZKTecoService, ZKTecoService>();
builder.Services.AddScoped<IKartOkuService, KartOkuService>();
builder.Services.AddScoped<IYemekhaneService, YemekhaneService>();
builder.Services.AddSingleton<ICihazService, CihazService>();
builder.Services.AddScoped<IOgrenciService, OgrenciService>();
builder.Services.AddScoped<IVeliProfilService, VeliProfilService>();
builder.Services.AddScoped<IServisProfilService, ServisProfilService>();
builder.Services.AddScoped<IOgretmenProfilService, OgretmenProfilService>();
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<IBirimService, BirimService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IZiyaretciService, ZiyaretciService>();
builder.Services.AddScoped<IKitapService, KitapService>();
builder.Services.AddScoped<IKitapDetayService, KitapDetayService>();
builder.Services.AddScoped<IKullaniciService, KullaniciService>();

// SMS
builder.Services.AddSmsAltyapisi(builder.Configuration);
builder.Services.AddScoped<ISmsGonderimService, SmsGonderimService>();

// Hosted services
builder.Services.AddHostedService<KartOkumaOlayIsleyiciService>();
builder.Services.AddHostedService<ZkBaglantiIzleyiciHostedService>();
builder.Services.AddHostedService<YemekhanePollingService>();
builder.Services.AddHostedService<BekleyenSmsRetryService>();

// SignalR + Cache
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

// Cookie Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Hesaplar/Giris";
        o.AccessDeniedPath = "/Hesaplar/YetkisizGiris";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
        o.Cookie.IsEssential = true;
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AdminOnly", p => p.RequireRole("Admin", "GenelAdmin"));
    o.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// --------------------
// Middleware
// --------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Tenant çözümleyici: cookie claim veya session'daki okulKodu'ndan connection string belirler
app.UseMiddleware<MvcTenantCozumleyiciMiddleware>();

// --------------------
// Endpoints
// --------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Hesaplar}/{action=Giris}/{id?}");

app.MapHub<KartOkuHub>("/kartOkuHub");

app.MapGet("/keep-alive", () => Results.Ok())
   .RequireAuthorization();

app.Run();
