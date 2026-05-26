using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class OgretmenRandevuYonetimView : ContentPage
    {
        private readonly OgretmenRandevuService _ogretmenRandevuService;

        public OgretmenRandevuYonetimView(OgretmenRandevuService ogretmenRandevuService)
        {
            InitializeComponent();
            _ogretmenRandevuService = ogretmenRandevuService;
            TarihSecici.MinimumDate = DateTime.Today;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await OgretmenRandevulariYukle();
        }

        private async Task OgretmenRandevulariYukle()
        {
            try
            {
                var randevular = await _ogretmenRandevuService.OgretmenRandevulariGetir();
                RandevuTakvimCollection.ItemsSource = randevular;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OGRETMEN RANDEVU LISTE HATASI]: {ex.Message}");
            }
        }

        private async void OnEkleClicked(object sender, EventArgs e)
        {
            if (TarihSecici.Date.Value < DateTime.Today)
            {
                await DisplayAlert("Uyarı", "Geçmiş tarih seçilemez.", "Tamam");
                return;
            }

            var baslangic = BaslangicSaati.Time.Value.ToString(@"hh\:mm");
            var bitis = BitisSaati.Time.Value.ToString(@"hh\:mm");

            if (BaslangicSaati.Time.Value >= BitisSaati.Time.Value)
            {
                await DisplayAlert("Uyarı", "Başlangıç saati bitiş saatinden önce olmalıdır.", "Tamam");
                return;
            }

            var sonuc = await _ogretmenRandevuService.OgretmenRandevuEkle(TarihSecici.Date.Value, baslangic, bitis);
            if (sonuc)
            {
                await OgretmenRandevulariYukle();
                TarihSecici.Date = DateTime.Today;
            }
            else
            {
                await DisplayAlert("Hata", "Randevu saati eklenirken bir sorun oluştu.", "Tamam");
            }
        }

        private async void OnSilClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is int ogretmenRandevuId)
            {
                var onay = await DisplayAlert("Onay", "Bu randevu saatini silmek istiyor musunuz?", "Evet", "Hayır");
                if (!onay) return;

                var sonuc = await _ogretmenRandevuService.OgretmenRandevuSil(ogretmenRandevuId);
                if (sonuc)
                    await OgretmenRandevulariYukle();
                else
                    await DisplayAlert("Hata", "Randevu saati silinirken bir sorun oluştu.", "Tamam");
            }
        }
    }
}
