using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using StudentTrackingSystem.Services;
using StudentTrackingSystem.Views;

namespace StudentTrackingSystem
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Servis kayıtları (Dependency Injection)
            builder.Services.AddSingleton<LoginService>();
            builder.Services.AddSingleton<ClassService>();
            builder.Services.AddSingleton<StudentService>();
            builder.Services.AddSingleton<UnitService>();

            // Sayfa kayıtları
            builder.Services.AddTransient<LoginView>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
