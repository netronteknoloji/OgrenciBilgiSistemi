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
                // v2: LockscreenVisibility eklendi. Android mevcut kanalın ayarını sonradan
                // değiştirmeye izin vermediği için yeni ID açılıp eski kanal siliniyor.
                var kanal = new NotificationChannel(
                    "obs_default_v2",
                    "Genel Bildirimler",
                    NotificationImportance.High)
                {
                    Description = "Randevu, duyuru ve hatırlatma bildirimleri",
                    LockscreenVisibility = NotificationVisibility.Public
                };

                var manager = (NotificationManager?)GetSystemService(NotificationService);
                manager?.CreateNotificationChannel(kanal);
                manager?.DeleteNotificationChannel("obs_default");
            }
        }
    }
}
