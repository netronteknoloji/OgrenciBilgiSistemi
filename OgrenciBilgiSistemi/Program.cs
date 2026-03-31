using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Abstractions;
using OgrenciBilgiSistemi.Infrastructure.FileStorage;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Hubs;
using OgrenciBilgiSistemi.Sms;
using OgrenciBilgiSistemi.Services;
using OgrenciBilgiSistemi.Services.BackgroundServices;
using OgrenciBilgiSistemi.Services.Implementations;
using OgrenciBilgiSistemi.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// --------------------
// Connection string
// --------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection bulunamadı.");

// --------------------
// DbContext
// --------------------
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// --------------------
// MVC + Global Authorize + Menu Yetki
// --------------------
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AuthorizeFilter());
    o.Filters.Add(typeof(OgrenciBilgiSistemi.Infrastructure.MenuYetkiFilter));
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
builder.Services.AddScoped<ICihazService, CihazService>();
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
    o.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
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

app.UseAuthentication();
app.UseAuthorization();

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