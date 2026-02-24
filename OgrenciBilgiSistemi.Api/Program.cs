using OgrenciBilgiSistemi.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Yapılandırma doğrulama
// --------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection yapılandırılmamış. " +
        "Environment variable veya appsettings.Development.json kullanın.");

// --------------------
// CORS — yalnızca yapılandırılmış originler
// --------------------
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        else
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
    });
});

// --------------------
// Servisler
// --------------------
builder.Services.AddControllers();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<ClassService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<UnitService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --------------------
// Middleware sırası
// --------------------
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("ConfiguredOrigins");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
