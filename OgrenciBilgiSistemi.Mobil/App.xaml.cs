using Microsoft.Maui.Handlers;                                                      // MAUI kontrol işleyicileri için gerekli kütüphane eklenir.
using OgrenciBilgiSistemi.Mobil.Services;                                               // TemelApiService.OturumSuresiDoldu olayı için gerekli.

namespace OgrenciBilgiSistemi.Mobil;                                                    // Uygulamanın ana isim uzayı tanımlanır.

public partial class App : Application                                              // Uygulamanın ana uygulama sınıfı tanımlanır.
{                                                                                   // Sınıf bloğu başlangıcı.
    public App()                                                                    // Sınıfın yapıcı metodu (constructor).
    {                                                                               // Metot bloğu başlangıcı.
        InitializeComponent();                                                      // XAML arayüz bileşenleri yüklenir.

        // 401 alındığında otomatik login ekranına yönlendir
        TemelApiService.OturumSuresiDoldu += async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.GoToAsync("///GirisView");
            });
        };

        #region Platform Bazlı Kontrol Özelleştirmeleri (Handler Mapping)

        #region Entry (Giriş Kutusu) Kenarlık Temizleme
        EntryHandler.Mapper.AppendToMapping("NoBorder", (handler, view) =>          // Entry kontrolü için yeni bir eşleme kuralı eklenir.
        {                                                                           // Anonim metot bloğu başlangıcı.
#if ANDROID                                                                         // Sadece Android işletim sistemi için derlenecek blok.
            handler.PlatformView.Background = null;                                 // Android arka plan nesnesi tamamen kaldırılır.
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);// Arka plan rengi saydam olarak ayarlanır.
            handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent); // Android alt çizgi rengi gizlenir.
#elif IOS || MACCATALYST                                                            // Apple platformları için derlenecek blok.
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;        // iOS üzerindeki standart çerçeve tipi 'Yok' yapılır.
#elif WINDOWS                                                                       // Windows masaüstü için derlenecek blok.
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0); // Windows çerçeve kalınlığı sıfıra indirilir.
            handler.PlatformView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent); // Windows arka planı saydamlaştırılır.
            handler.PlatformView.Resources["TextControlBorderThemeThicknessFocused"] = new Microsoft.UI.Xaml.Thickness(0); // Odaklanma anındaki Windows çerçevesi kaldırılır.
            handler.PlatformView.Resources["TextControlBorderThemeThickness"] = new Microsoft.UI.Xaml.Thickness(0); // Varsayılan Windows tema çerçeve kalınlığı sıfırlanır.
            handler.PlatformView.MinWidth = 0;                                      // Kontrolün minimum genişlik kısıtlaması kaldırılır.
            handler.PlatformView.MinHeight = 0;                                     // Kontrolün minimum yükseklik kısıtlaması kaldırılır.
#endif                                                                              // Koşullu derleme sonu.
        });                                                                         // Mapping işlemi sonlandırılır.
        #endregion

        #region Picker (Seçici) Kenarlık Temizleme
        PickerHandler.Mapper.AppendToMapping("NoBorder", (handler, view) =>         // Picker kontrolü için eşleme kuralı tanımlanır.
        {                                                                           // Anonim metot bloğu başlangıcı.
#if ANDROID                                                                         // Android özel kodu.
            handler.PlatformView.Background = null;                                 // Android'deki varsayılan seçici arka planı kaldırılır.
            handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent); // Seçim çizgisinin rengi saydam yapılır.
#elif IOS || MACCATALYST                                                            // iOS özel kodu.
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;        // iOS kenarlık stili kaldırılır.
#elif WINDOWS                                                                       // Windows özel kodu.
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0); // Windows kenarlık kalınlığı sıfırlanır.
#endif                                                                              // Koşullu derleme sonu.
        });                                                                         // Mapping işlemi sonlandırılır.
        #endregion

        #endregion

        #region Başlangıç Sayfası Ayarı
        MainPage = new AppShell();                                                  // Uygulamanın ana navigasyon yapısı olan AppShell başlatılır.
        #endregion
    }                                                                               // Yapıcı metot sonu.
}                                                                                   // Sınıf sonu.