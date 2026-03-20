using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OgrenciBilgiSistemi.Api.Services;
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
// CORS — mobil istemciler dahil tüm originlere izin ver
// --------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        policy.AllowAnyOrigin()
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
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
    });
// AdminOnly policy: adminMi claim'i "true" olan kullanıcılar
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly", p => p.RequireClaim("adminMi", "true"));
});

// --------------------
// Servisler
// --------------------
builder.Services.AddControllers();
builder.Services.AddScoped<GirisService>();
builder.Services.AddScoped<SinifService>();
builder.Services.AddScoped<OgrenciService>();
builder.Services.AddScoped<BirimService>();
builder.Services.AddScoped<GecisKayitService>();
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

// Kimlik doğrulama ve yetkilendirme middleware'i
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
