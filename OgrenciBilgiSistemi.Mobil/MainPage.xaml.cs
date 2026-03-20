#region Gerekli Kütüphanelerin Tanımlanması
using Microsoft.Maui.Accessibility;                                         // Erişilebilirlik ve ekran okuyucu özellikleri için gerekli kütüphane.
#endregion

namespace OgrenciBilgiSistemi.Mobil                                             // Projenin genel organizasyon yapısını belirleyen isim uzayı.
{                                                                           // İsim uzayı kapsamı başlangıcı.
    public partial class MainPage : ContentPage                             // MainPage sınıfı, ContentPage sınıfından türetilen kısmi bir sınıftır.
    {                                                                       // Sınıf gövdesi başlangıcı.

        #region Sınıf Düzeyi Değişkenler
        int count = 0;                                                      // Tıklama sayısını hafızada tutmak için kullanılan tam sayı değişkeni.
        #endregion

        #region Yapıcı Metot (Constructor)
        public MainPage()                                                   // Sayfa ilk oluşturulduğunda çalışan yapıcı metot.
        {                                                                   // Yapıcı metot kapsamı başlangıcı.
            InitializeComponent();                                          // XAML tarafında tasarlanan arayüz bileşenlerini koda bağlar ve yükler.
        }                                                                   // Yapıcı metot kapsamı sonu.
        #endregion

        #region Kullanıcı Etkileşim Olayları
        private void OnCounterClicked(object sender, EventArgs e)           // Buton tıklama olayını yakalayan metot tanımlaması.
        {                                                                   // Metot gövdesi başlangıcı.
            count++;                                                        // Tıklama sayısını tutan değişkenin değerini bir birim artırır.

            if (count == 1)                                                 // Eğer buton tam olarak bir kez tıklandıysa bu blok çalışır.
            {                                                               // Şartlı blok başlangıcı.
                CounterBtn.Text = $"Clicked {count} time";                  // Buton üzerindeki metni tekil ifade (time) kullanarak günceller.
            }                                                               // Şartlı blok sonu.
            else                                                            // Bir dışındaki tüm diğer tıklama sayıları için bu blok çalışır.
            {                                                               // Şartlı blok başlangıcı.
                CounterBtn.Text = $"Clicked {count} times";                 // Buton üzerindeki metni çoğul ifade (times) kullanarak günceller.
            }                                                               // Şartlı blok sonu.

            SemanticScreenReader.Announce(CounterBtn.Text);                 // Güncellenen metni görme engelli kullanıcılar için sesli olarak duyurur.
        }                                                                   // Metot gövdesi sonu.
        #endregion

    }                                                                       // Sınıf gövdesi sonu.
}                                                                           // İsim uzayı kapsamı sonu.