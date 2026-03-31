using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class ServisEkraniView : ContentPage
    {
        private readonly ServisService _servisService;
        private List<OgrenciGorunumModel> _tumOgrenciler = new();
        private int? _servisId;

        public ServisEkraniView(ServisService servisService)
        {
            try
            {
                InitializeComponent();
                _servisService = servisService;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServisEkraniView Init Hatası: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                WelcomeLabel.Text = $"Merhaba, {KullaniciOturum.AdSoyad}";

                _servisId = KullaniciOturum.ServisId;
                if (!_servisId.HasValue)
                {
                    OgrenciSayisiLabel.Text = "Servis ataması bulunamadı";
                    return;
                }

                // Servis bilgisini getir (plaka)
                var servis = await _servisService.ServisGetir(_servisId.Value);
                if (servis != null)
                    PlakaLabel.Text = $"Plaka: {servis.Plaka}";

                // Öğrencileri getir ve OgrenciGorunumModel olarak wrap et
                var ogrenciler = await _servisService.ServisOgrencileriGetir(_servisId.Value);
                _tumOgrenciler = ogrenciler.Select(o => new OgrenciGorunumModel
                {
                    OgrenciData = o,
                    ServisDurumId = 0 // Periyot seçilene kadar seçimsiz
                }).ToList();

                OgrenciCollection.ItemsSource = _tumOgrenciler;
                OgrenciSayisiLabel.Text = $"{_tumOgrenciler.Count} öğrenci";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServisEkrani Yükleme Hatası: {ex.Message}");
                OgrenciSayisiLabel.Text = "Veriler yüklenemedi";
            }
        }

        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var filtre = e.NewTextValue?.Trim();
                if (string.IsNullOrEmpty(filtre))
                {
                    OgrenciCollection.ItemsSource = _tumOgrenciler;
                }
                else
                {
                    OgrenciCollection.ItemsSource = _tumOgrenciler
                        .Where(o => o.OgrenciData?.OgrenciAdSoyad != null &&
                                    o.OgrenciData.OgrenciAdSoyad.Contains(filtre, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }
            catch { }
        }

        private async void OnPeriyotChanged(object sender, EventArgs e)
        {
            try
            {
                if (PeriyotPicker.SelectedIndex == -1 || !_servisId.HasValue) return;
                int periyot = PeriyotPicker.SelectedIndex + 1; // 1=Sabah, 2=Akşam

                // Mevcut yoklamayı API'den çek
                var mevcutYoklama = await _servisService.MevcutServisYoklamaGetir(_servisId.Value, periyot);
                bool kayitVar = mevcutYoklama != null && mevcutYoklama.Count > 0;

                // Uyarı ve buton durumlarını güncelle
                StatusWarningFrame.IsVisible = kayitVar;
                BtnKaydet.IsVisible = !kayitVar;
                BtnGuncelle.IsVisible = kayitVar;

                if (_tumOgrenciler != null)
                {
                    foreach (var vm in _tumOgrenciler)
                    {
                        if (kayitVar && mevcutYoklama.TryGetValue(vm.OgrenciData.OgrenciId, out int durumId))
                            vm.ServisDurumId = durumId;
                        else
                            vm.ServisDurumId = 1; // Bindi
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Periyot Değişim Hatası: {ex.Message}");
            }
        }

        private void OnServisDurumTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (sender is Border border && border.BindingContext is OgrenciGorunumModel vm && e.Parameter != null)
                {
                    if (int.TryParse(e.Parameter.ToString(), out int durumId))
                    {
                        vm.ServisDurumId = durumId;
                    }
                }
            }
            catch { }
        }

        private async void OnYoklamaKaydet(object sender, EventArgs e)
        {
            await YoklamaIsle(guncelleme: false);
        }

        private async void OnYoklamaGuncelle(object sender, EventArgs e)
        {
            bool onay = await DisplayAlert("Onay", "Mevcut yoklama kaydını değiştirmek istediğinize emin misiniz?", "Evet", "Hayır");
            if (onay)
            {
                await YoklamaIsle(guncelleme: true);
            }
        }

        private async Task YoklamaIsle(bool guncelleme)
        {
            try
            {
                if (PeriyotPicker.SelectedIndex == -1)
                {
                    await DisplayAlert("Uyarı", "Lütfen önce periyot seçiniz!", "Tamam");
                    return;
                }

                if (!_servisId.HasValue)
                {
                    await DisplayAlert("Hata", "Servis bilgisi bulunamadı.", "Tamam");
                    return;
                }

                // Durum seçilmemiş öğrenci var mı kontrol et
                var secilmemis = _tumOgrenciler.Where(o => o.ServisDurumId == 0).ToList();
                if (secilmemis.Count > 0)
                {
                    await DisplayAlert("Uyarı", $"{secilmemis.Count} öğrenci için durum seçilmemiş. Lütfen tüm öğrenciler için Bindi veya Binmedi seçiniz.", "Tamam");
                    return;
                }

                int periyot = PeriyotPicker.SelectedIndex + 1;
                var yoklamaVerisi = _tumOgrenciler
                    .Select(vm => (vm.OgrenciData.OgrenciId, vm.ServisDurumId))
                    .ToList();

                await _servisService.ServisYoklamaKaydet(yoklamaVerisi, periyot);

                string mesaj = guncelleme ? "Yoklama güncellendi." : "Yoklama başarıyla kaydedildi.";
                await DisplayAlert("Bilgi", mesaj, "Tamam");

                // Kayıt sonrası uyarı ve buton durumlarını güncelle
                StatusWarningFrame.IsVisible = true;
                BtnKaydet.IsVisible = false;
                BtnGuncelle.IsVisible = true;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"İşlem sırasında sorun çıktı: {ex.Message}", "Tamam");
            }
        }
    }
}
