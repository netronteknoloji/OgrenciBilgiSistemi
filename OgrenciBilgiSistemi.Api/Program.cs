using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using OgrenciBilgiSistemi.Api.Services;
using OgrenciBilgiSistemi.Sms;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Yapılandırma doğrulama
// --------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection yapılandırılmamış. " +
        "Environment variable veya appsettings.Development.json kullanın.");

var jwtKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "Jwt:SecretKey yapılandırılmamış. " +
        "appsettings.Development.json veya ortam değişkeni kullanın.");

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
    opts.AddPolicy("AdminOnly", p => p.RequireClaim("rol", "Admin"));
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

// SMS
builder.Services.AddSmsAltyapisi(builder.Configuration);
builder.Services.AddScoped<YoklamaSmsBildirimService>();
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

// MVC projesinin wwwroot/uploads klasörünü /uploads altında sun
var mvcWwwRoot = Path.Combine(app.Environment.ContentRootPath, "..", "OgrenciBilgiSistemi", "wwwroot");
if (Directory.Exists(mvcWwwRoot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.GetFullPath(mvcWwwRoot)),
        RequestPath = ""
    });
}

// Kimlik doğrulama ve yetkilendirme middleware'i
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
