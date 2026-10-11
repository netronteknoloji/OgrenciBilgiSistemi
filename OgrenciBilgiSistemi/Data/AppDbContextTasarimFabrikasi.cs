using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OgrenciBilgiSistemi.Shared.Models;

namespace OgrenciBilgiSistemi.Data
{
    /// <summary>
    /// Yalnızca EF araçları (migrations add/script) için: çalışma zamanında tenant'sız DbContext
    /// bağlanmaz, tasarım zamanında ilk okulun bağlantısı (yoksa yerel bir yer tutucu) kullanılır.
    /// </summary>
    public class AppDbContextTasarimFabrikasi : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var baglanti = config.GetSection("Okullar").Get<List<OkulBilgiAyari>>()?
                .Select(o => o.ConnectionString)
                .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c))
                ?? "Server=.;Database=OgrenciBilgiSistemiTasarim;Trusted_Connection=True;TrustServerCertificate=True";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(baglanti)
                .Options;

            return new AppDbContext(options);
        }
    }
}
