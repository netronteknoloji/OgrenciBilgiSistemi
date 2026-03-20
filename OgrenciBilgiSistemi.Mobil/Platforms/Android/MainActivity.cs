using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace OgrenciBilgiSistemi.Mobil
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
        ScreenOrientation = ScreenOrientation.Portrait)] // Uygulamayı dikey moda sabitler
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Uygulamanın en üstteki durum çubuğuyla (StatusBar) bütünleşmesini sağlar
            if (Window != null)
            {
                // İçeriği durum çubuğunun altına kaydırır
                Window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);

                // Durum çubuğunu tamamen şeffaf yapar
                Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            }
        }
    }
}