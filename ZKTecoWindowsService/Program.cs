using Microsoft.EntityFrameworkCore;
using ZKTecoWindowsService.Data;
using ZKTecoWindowsService.Models.Options;
using ZKTecoWindowsService.Services;

Host.CreateDefaultBuilder(args)
    .UseWindowsService() // Windows Service olarak çalýþ
    .ConfigureLogging((ctx, logging) =>
    {
        logging.ClearProviders();
        logging.AddEventLog(o => o.SourceName = "ZKTecoWorker"); // Event Viewer
        logging.AddSimpleConsole(); // (opsiyonel) konsola da yaz
    })
    .ConfigureServices((ctx, services) =>
    {
        // DbContext (örnek: SQL Server)
        var cs = ctx.Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContextPool<AppDbContext>(opt =>
        {
            opt.UseSqlServer(cs);
            // opt.EnableSensitiveDataLogging(false);
        });

        // Worker + baðýmlýlýklar
        services.AddHostedService<Worker>();
        services.AddSingleton<IZkClient, ZkClient>();

        // Options
        services.Configure<SmsSettings>(ctx.Configuration.GetSection("SmsSettings"));

        // HttpClient tabanlý SMS servisi
        services.AddHttpClient<IOgrenciSmsService, OgrenciSmsService>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(30);
        });
    })
    .Build()
    .Run();