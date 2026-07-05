using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class SinifListeGorunumModel : ObservableObject
    {
        private readonly SinifService _sinifService;
        private readonly OgrenciService _ogrenciService;
        private readonly BildirimService _bildirimService;
        private readonly RandevuService _randevuService;
        private readonly OgretmenRandevuService _ogretmenRandevuService;
        private readonly DuyuruService _duyuruService;
        private IReadOnlyList<SinifGorunumModel> _tumSiniflar = [];

        [ObservableProperty] private IReadOnlyList<SinifGorunumModel> filtreliSiniflar = [];
        [ObservableProperty] private string aramaMetni = string.Empty;
        [ObservableProperty] private string karsilamaMetni = "Merhaba";
        [ObservableProperty] private bool bildirimBadgeGorunur;
        [ObservableProperty] private string bildirimSayiMetni = string.Empty;

        public SinifListeGorunumModel(
            SinifService sinifService,
            OgrenciService ogrenciService,
            BildirimService bildirimService,
            RandevuService randevuService,
            OgretmenRandevuService ogretmenRandevuService,
            DuyuruService duyuruService)
        {
            _sinifService = sinifService;
            _ogrenciService = ogrenciService;
            _bildirimService = bildirimService;
            _randevuService = randevuService;
            _ogretmenRandevuService = ogretmenRandevuService;
            _duyuruService = duyuruService;
        }

        partial void OnAramaMetniChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                FiltreliSiniflar = _tumSiniflar;
            else
            {
                var lower = value.ToLower();
                FiltreliSiniflar = _tumSiniflar.Where(vm => vm.Ad?.ToLower().Contains(lower) == true).ToList();
            }
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            string displayName = string.IsNullOrWhiteSpace(KullaniciOturum.AdSoyad) ? "Kullanıcı" : KullaniciOturum.AdSoyad;
            KarsilamaMetni = $"Merhaba {displayName}";

            try
            {
                var classes = await _sinifService.TumSiniflariOgrenciSayisiIleGetirAsync();
                if (classes != null)
                {
                    _tumSiniflar = classes;
                    FiltreliSiniflar = classes;
                }
            }
            catch
            {
                await Application.Current!.MainPage!.DisplayAlert("Bağlantı Hatası", "Sınıf listesi sunucudan alınamadı. Lütfen internetinizi kontrol edin.", "Tamam");
            }

            try
            {
                var sayi = await _bildirimService.OkunmamisSayisiGetir();
                BildirimBadgeGorunur = sayi > 0;
                BildirimSayiMetni = sayi > 9 ? "9+" : sayi.ToString();
            }
            catch { }
        }

        [RelayCommand]
        async Task SinifSecAsync(SinifGorunumModel vm)
        {
            if (vm?.SinifVerisi == null) return;
            var listeVm = new OgrenciListeGorunumModel(vm.SinifVerisi.BirimId, vm.SinifVerisi.BirimAd, _ogrenciService);
            await Shell.Current.Navigation.PushAsync(new OgrenciListeView(listeVm));
        }

        [RelayCommand]
        async Task RandevularAsync()
        {
            var vm = new RandevuListeGorunumModel(_randevuService, _bildirimService);
            await Shell.Current.Navigation.PushAsync(new RandevuListeView(vm));
        }

        [RelayCommand]
        async Task OgretmenRandevuAsync()
        {
            var vm = new OgretmenRandevuYonetimGorunumModel(_ogretmenRandevuService);
            await Shell.Current.Navigation.PushAsync(new OgretmenRandevuYonetimView(vm));
        }

        [RelayCommand]
        async Task BildirimlerAsync()
        {
            var vm = new BildirimListeGorunumModel(_bildirimService, _randevuService);
            await Shell.Current.Navigation.PushAsync(new BildirimListeView(vm));
        }

        [RelayCommand]
        async Task DuyuruYapAsync()
        {
            var vm = new OgretmenDuyuruOlusturGorunumModel(_duyuruService);
            await Shell.Current.Navigation.PushAsync(new OgretmenDuyuruOlusturView(vm));
        }
    }
}
