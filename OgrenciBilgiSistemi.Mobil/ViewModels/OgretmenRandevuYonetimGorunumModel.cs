using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class OgretmenRandevuYonetimGorunumModel : ObservableObject
    {
        private readonly OgretmenRandevuService _service;

        [ObservableProperty] private IReadOnlyList<OgretmenRandevu> slotlar = [];
        [ObservableProperty] private DateTime secilenTarih = DateTime.Today;
        [ObservableProperty] private TimeSpan secilenBaslangicSaati = new TimeSpan(9, 0, 0);
        [ObservableProperty] private TimeSpan secilenBitisSaati = new TimeSpan(10, 0, 0);

        public DateTime MinimumTarih { get; } = DateTime.Today;

        public OgretmenRandevuYonetimGorunumModel(OgretmenRandevuService service)
        {
            _service = service;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                Slotlar = await _service.OgretmenRandevulariGetir();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OGRETMEN RANDEVU HATASI]: {ex.Message}");
            }
        }

        [RelayCommand]
        async Task EkleAsync()
        {
            if (SecilenTarih.Date < DateTime.Today)
            {
                await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Geçmiş tarih seçilemez.", "Tamam");
                return;
            }
            if (SecilenBaslangicSaati >= SecilenBitisSaati)
            {
                await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Başlangıç saati bitiş saatinden önce olmalıdır.", "Tamam");
                return;
            }

            var baslangic = SecilenBaslangicSaati.ToString(@"hh\:mm");
            var bitis = SecilenBitisSaati.ToString(@"hh\:mm");

            var sonuc = await _service.OgretmenRandevuEkle(SecilenTarih, baslangic, bitis);
            if (sonuc)
            {
                SecilenTarih = DateTime.Today;
                await YukleAsync();
            }
            else
            {
                await Application.Current!.MainPage!.DisplayAlert("Hata", "Randevu saati eklenirken bir sorun oluştu.", "Tamam");
            }
        }

        [RelayCommand]
        async Task SilAsync(int ogretmenRandevuId)
        {
            var onay = await Application.Current!.MainPage!.DisplayAlert("Onay", "Bu randevu saatini silmek istiyor musunuz?", "Evet", "Hayır");
            if (!onay) return;

            var sonuc = await _service.OgretmenRandevuSil(ogretmenRandevuId);
            if (sonuc)
                await YukleAsync();
            else
                await Application.Current!.MainPage!.DisplayAlert("Hata", "Randevu saati silinirken bir sorun oluştu.", "Tamam");
        }
    }
}
