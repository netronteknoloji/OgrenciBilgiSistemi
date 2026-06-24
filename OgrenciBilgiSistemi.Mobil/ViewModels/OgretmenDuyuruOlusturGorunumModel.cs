using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class OgretmenDuyuruOlusturGorunumModel : ObservableObject
    {
        private readonly DuyuruService _duyuruService;

        [ObservableProperty] private string baslik = string.Empty;
        [ObservableProperty] private string icerik = string.Empty;
        [ObservableProperty] private string hataMesaji = string.Empty;
        [ObservableProperty] private bool hataGorunur;
        [ObservableProperty] private bool butonEtkin = true;

        public OgretmenDuyuruOlusturGorunumModel(DuyuruService duyuruService)
        {
            _duyuruService = duyuruService;
        }

        [RelayCommand]
        async Task YayinlaAsync()
        {
            var b = Baslik.Trim();
            var i = Icerik.Trim();

            if (string.IsNullOrEmpty(b)) { HataGoster("Başlık zorunludur."); return; }
            if (string.IsNullOrEmpty(i)) { HataGoster("İçerik zorunludur."); return; }

            HataGorunur = false;
            ButonEtkin = false;
            try
            {
                var (basarili, mesaj) = await _duyuruService.Olustur(b, i);
                if (basarili)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Duyuru velilere gönderildi.", "Tamam");
                    Baslik = string.Empty;
                    Icerik = string.Empty;
                    await Shell.Current.Navigation.PopAsync();
                }
                else
                {
                    HataGoster(mesaj ?? "Duyuru yayınlanamadı.");
                }
            }
            finally
            {
                ButonEtkin = true;
            }
        }

        private void HataGoster(string mesaj)
        {
            HataMesaji = mesaj;
            HataGorunur = true;
        }
    }
}
