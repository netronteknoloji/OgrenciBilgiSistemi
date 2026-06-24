using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class VeliAnaSayfaGorunumModel : ObservableObject
    {
        private readonly VeliService _veliService;
        private readonly BildirimService _bildirimService;
        private readonly DuyuruService _duyuruService;
        private readonly RandevuService _randevuService;

        [ObservableProperty] private string karsilamaMetni = "Merhaba";
        [ObservableProperty] private string cocukSayisiMetni = "Yükleniyor...";
        [ObservableProperty] private IReadOnlyList<Ogrenci> cocuklar = [];
        [ObservableProperty] private string bildirimSayiMetni = string.Empty;
        [ObservableProperty] private bool bildirimBadgeGorunur;
        [ObservableProperty] private string duyuruSayiMetni = string.Empty;
        [ObservableProperty] private bool duyuruBadgeGorunur;

        public VeliAnaSayfaGorunumModel(
            VeliService veliService,
            BildirimService bildirimService,
            DuyuruService duyuruService,
            RandevuService randevuService)
        {
            _veliService = veliService;
            _bildirimService = bildirimService;
            _duyuruService = duyuruService;
            _randevuService = randevuService;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                KarsilamaMetni = $"Merhaba, {KullaniciOturum.AdSoyad}";

                var cocuklar = await _veliService.CocuklarimiGetir();
                Cocuklar = cocuklar;
                CocukSayisiMetni = cocuklar.Count > 0
                    ? $"{cocuklar.Count} çocuk kayıtlı"
                    : "Kayıtlı öğrenci bulunamadı";

                await BildirimBadgeGuncelleAsync();
                await DuyuruBadgeGuncelleAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VeliAnaSayfa Yükleme Hatası: {ex.Message}");
                CocukSayisiMetni = "Veriler yüklenemedi";
            }
        }

        [RelayCommand]
        async Task CocukSecAsync(Ogrenci ogrenci)
            => await Shell.Current.Navigation.PushAsync(new OgrenciDetayView(ogrenci.OgrenciId));

        [RelayCommand]
        async Task RandevularAsync()
        {
            var vm = new RandevuListeGorunumModel(_randevuService, _bildirimService);
            await Shell.Current.Navigation.PushAsync(new RandevuListeView(vm));
        }

        [RelayCommand]
        async Task BildirimlerAsync()
        {
            var vm = new BildirimListeGorunumModel(_bildirimService, _randevuService);
            await Shell.Current.Navigation.PushAsync(new BildirimListeView(vm));
        }

        [RelayCommand]
        async Task DuyurularAsync()
        {
            var vm = new VeliDuyurularGorunumModel(_duyuruService);
            await Shell.Current.Navigation.PushAsync(new VeliDuyurularView(vm));
        }

        private async Task BildirimBadgeGuncelleAsync()
        {
            try
            {
                var sayi = await _bildirimService.OkunmamisSayisiGetir();
                BildirimBadgeGorunur = sayi > 0;
                BildirimSayiMetni = sayi > 9 ? "9+" : sayi.ToString();
            }
            catch { }
        }

        private async Task DuyuruBadgeGuncelleAsync()
        {
            try
            {
                var sayi = await _duyuruService.OkunmamisSayisiGetir();
                DuyuruBadgeGorunur = sayi > 0;
                DuyuruSayiMetni = sayi > 9 ? "9+" : sayi.ToString();
            }
            catch { }
        }
    }
}
