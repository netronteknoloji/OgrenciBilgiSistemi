using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;
using System.Collections.ObjectModel;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class RandevuListeGorunumModel : ObservableObject
    {
        private readonly RandevuService _randevuService;
        private readonly BildirimService _bildirimService;
        private int _mevcutSayfa = 1;
        private bool _dahaFazlaVar = true;
        private bool _yukleniyor;

        [ObservableProperty] private ObservableCollection<RandevuGorunumModel> randevular = [];
        [ObservableProperty] private bool yenileniyor;

        // Rol paylaşımlı ekran: giren rolün tema rengiyle boyanır (rol login sonrası değişmez)
        public Color TemaRenk => RolTema.VurguRenk;
        public Brush TemaGradyan => RolTema.BaslikGradyan;

        public RandevuListeGorunumModel(RandevuService randevuService, BildirimService bildirimService)
        {
            _randevuService = randevuService;
            _bildirimService = bildirimService;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            Randevular.Clear();
            _mevcutSayfa = 1;
            _dahaFazlaVar = true;
            await RandevulariEkleAsync();
        }

        [RelayCommand]
        async Task YenileAsync()
        {
            Yenileniyor = true;
            try
            {
                Randevular.Clear();
                _mevcutSayfa = 1;
                _dahaFazlaVar = true;
                await RandevulariEkleAsync();
            }
            finally
            {
                Yenileniyor = false;
            }
        }

        [RelayCommand]
        async Task DahaFazlaYukleAsync()
        {
            await RandevulariEkleAsync();
        }

        [RelayCommand]
        async Task RandevuSecAsync(RandevuGorunumModel gorunum)
        {
            var vm = RandevuDetayGorunumModel.FromRandevu(_randevuService, gorunum.Randevu);
            await Shell.Current.Navigation.PushAsync(new RandevuDetayView(vm));
        }

        [RelayCommand]
        async Task YeniRandevuAsync()
        {
            var view = IPlatformApplication.Current.Services.GetRequiredService<RandevuOlusturView>();
            await Shell.Current.Navigation.PushAsync(view);
        }

        [RelayCommand]
        async Task BildirimlerAsync()
        {
            var vm = new BildirimListeGorunumModel(_bildirimService, _randevuService);
            await Shell.Current.Navigation.PushAsync(new BildirimListeView(vm));
        }

        private async Task RandevulariEkleAsync()
        {
            if (_yukleniyor || !_dahaFazlaVar) return;
            _yukleniyor = true;
            try
            {
                var randevular = await _randevuService.RandevulariGetir(_mevcutSayfa);
                if (randevular.Count < 5)
                    _dahaFazlaVar = false;

                foreach (var r in randevular)
                    Randevular.Add(new RandevuGorunumModel(r));
                _mevcutSayfa++;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RANDEVU LISTE HATASI]: {ex.Message}");
                await Application.Current!.MainPage!.DisplayAlert("Hata", $"Randevular yüklenemedi.\n{ex.Message}", "Tamam");
            }
            finally
            {
                _yukleniyor = false;
            }
        }
    }
}
