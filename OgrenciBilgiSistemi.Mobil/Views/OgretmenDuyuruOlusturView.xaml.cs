using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class OgretmenDuyuruOlusturView : ContentPage
    {
        private readonly DuyuruService _duyuruService;

        public OgretmenDuyuruOlusturView(DuyuruService duyuruService)
        {
            InitializeComponent();
            _duyuruService = duyuruService;
        }

        private async void OnYayinlaClicked(object sender, EventArgs e)
        {
            var baslik = (BaslikEntry.Text ?? string.Empty).Trim();
            var icerik = (IcerikEditor.Text ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(baslik))
            {
                HataGoster("Başlık zorunludur.");
                return;
            }
            if (string.IsNullOrEmpty(icerik))
            {
                HataGoster("İçerik zorunludur.");
                return;
            }

            YayinlaButonu.IsEnabled = false;
            try
            {
                var (basarili, mesaj) = await _duyuruService.Olustur(baslik, icerik);
                if (basarili)
                {
                    await DisplayAlert("Başarılı", "Duyuru velilere gönderildi.", "Tamam");
                    BaslikEntry.Text = string.Empty;
                    IcerikEditor.Text = string.Empty;
                    HataLabel.IsVisible = false;
                    await Navigation.PopAsync();
                }
                else
                {
                    HataGoster(mesaj ?? "Duyuru yayınlanamadı.");
                }
            }
            finally
            {
                YayinlaButonu.IsEnabled = true;
            }
        }

        private void HataGoster(string mesaj)
        {
            HataLabel.Text = mesaj;
            HataLabel.IsVisible = true;
        }
    }
}
