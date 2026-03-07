using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Abstractions;
using OgrenciBilgiSistemi.Infrastructure.FileStorage;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Hubs;
using OgrenciBilgiSistemi.Services;
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
// MVC + Global Authorize
// --------------------
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AuthorizeFilter());
});

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
builder.Services.AddScoped<IOgrenciVeliService, OgrenciVeliService>();
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<IBirimService, BirimService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IPersonelService, PersonelService>();
builder.Services.AddScoped<IZiyaretciService, ZiyaretciService>();
builder.Services.AddScoped<IKitapService, KitapService>();
builder.Services.AddScoped<IKitapDetayService, KitapDetayService>();

// Hosted services
builder.Services.AddHostedService<CardReadEventHandlerService>();
builder.Services.AddHostedService<ZkConnectionMonitorHostedService>();

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