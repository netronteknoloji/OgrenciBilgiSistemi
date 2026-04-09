using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using OgrenciBilgiSistemi.Api.Middleware;
using OgrenciBilgiSistemi.Api.Services;
using OgrenciBilgiSistemi.Shared.Models;
using OgrenciBilgiSistemi.Shared.Services;
using OgrenciBilgiSistemi.Sms;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Yapılandırma doğrulama
// --------------------
var okullar = builder.Configuration.GetSection("Okullar").Get<List<OkulBilgiAyari>>();
if (okullar is null || okullar.Count == 0)
    throw new InvalidOperationException(
        "Okullar yapılandırılmamış. appsettings.json içinde Okullar bölümünü ekleyin.");

var jwtKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "Jwt:SecretKey yapılandırılmamış. " +
        "appsettings.Development.json veya ortam değişkeni kullanın.");

// --------------------
// Multi-tenant yapılandırma
// --------------------
builder.Services.Configure<List<OkulBilgiAyari>>(builder.Configuration.GetSection("Okullar"));
builder.Services.AddSingleton<OkulYapilandirmaServisi>();
builder.Services.AddScoped<TenantBaglami>();

// --------------------
// CORS — appsettings'ten yapılandırılmış origin'ler
// --------------------
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins").Get<string[]>();

    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        if (allowedOrigins is { Length: > 0 })
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        else
            // Yapılandırma eksikse hiçbir origin'e izin verme (güvenli varsayılan)
            policy.WithOrigins("https://localhost")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
    });
});

// --------------------
// JWT Kimlik Doğrulama
// --------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
    });
// AdminOnly policy: rol claim'i "Admin" olan kullanıcılar
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly", p => p.RequireClaim("rol", "Admin", "GenelAdmin"));
});

// --------------------
// Servisler
// --------------------
builder.Services.AddControllers();
builder.Services.AddScoped<GirisService>();
builder.Services.AddSingleton<RefreshTokenService>();
builder.Services.AddScoped<SinifService>();
builder.Services.AddScoped<OgrenciService>();
builder.Services.AddScoped<BirimService>();
builder.Services.AddScoped<GecisKayitService>();
builder.Services.AddScoped<ServisService>();

// Rate Limiting — anonim arama endpointleri için IP bazlı sınırlama
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("arama", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// SMS
builder.Services.AddSmsAltyapisi(builder.Configuration);
builder.Services.AddScoped<YoklamaSmsBildirimService>();
builder.Services.AddHostedService<BekleyenYoklamaSmsRetryService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --------------------
// Middleware sırası
// --------------------
app.UseHttpsRedirection();
app.UseCors("ConfiguredOrigins");

app.UseSwagger();
app.UseSwaggerUI();

// MVC projesinin wwwroot klasörünü statik dosya olarak sun
var mvcWwwRoot = app.Configuration["MvcWwwRootPath"];
if (string.IsNullOrWhiteSpace(mvcWwwRoot))
{
    // Production (IIS) ve development fallback yolları
    var adayYollar = new[]
    {
        @"C:\inetpub\wwwroot\obs\wwwroot",
        Path.Combine(app.Environment.ContentRootPath, "..", "OgrenciBilgiSistemi", "wwwroot")
    };
    mvcWwwRoot = adayYollar.FirstOrDefault(Directory.Exists);
}
if (!string.IsNullOrWhiteSpace(mvcWwwRoot) && Directory.Exists(mvcWwwRoot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.GetFullPath(mvcWwwRoot)),
        RequestPath = ""
    });
}
else
{
    app.Logger.LogWarning("MVC wwwroot klasörü bulunamadı. Resimler sunulamayacak. MvcWwwRootPath ayarını kontrol edin.");
}

// Rate limiting
app.UseRateLimiter();

// Kimlik doğrulama ve yetkilendirme middleware'i
app.UseAuthentication();
app.UseAuthorization();

// Tenant çözümleyici: JWT'deki okulKodu claim'inden connection string belirler
app.UseMiddleware<TenantCozumleyiciMiddleware>();

app.MapControllers();
app.Run();
