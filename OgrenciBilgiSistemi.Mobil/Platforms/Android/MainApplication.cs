using Android.App;
using Android.OS;
using Android.Runtime;

namespace OgrenciBilgiSistemi.Mobil
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override void OnCreate()
        {
            base.OnCreate();

            // Firebase Android: google-services.json varsa native SDK Application context ile otomatik
            // başlar. Plugin.Firebase.Core'un Activity-bazlı Initialize çağrısı MainActivity'de yapılır.

            // Bildirim kanalı (Android 8+ — Oreo zorunlu)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var kanal = new NotificationChannel(
                    "obs_default",
                    "Genel Bildirimler",
                    NotificationImportance.High)
                {
                    Description = "Randevu, duyuru ve hatırlatma bildirimleri"
                };

                var manager = (NotificationManager?)GetSystemService(NotificationService);
                manager?.CreateNotificationChannel(kanal);
            }
        }
    }
}
