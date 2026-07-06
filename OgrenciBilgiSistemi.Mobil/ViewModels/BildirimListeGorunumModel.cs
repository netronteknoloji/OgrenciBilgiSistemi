using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;
using System.Collections.ObjectModel;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class BildirimListeGorunumModel : ObservableObject
    {
        private readonly BildirimService _bildirimService;
        private readonly RandevuService _randevuService;
        private int _sayfaNo = 1;

        [ObservableProperty] private ObservableCollection<BildirimGorunumModel> bildirimler = [];
        [ObservableProperty] private string altBaslik = string.Empty;
        [ObservableProperty] private bool dahaFazlaVar = true;

        // Rol paylaşımlı ekran: giren rolün tema rengiyle boyanır (rol login sonrası değişmez)
        public Color TemaRenk => RolTema.VurguRenk;
        public Brush TemaGradyan => RolTema.BaslikGradyan;
        private bool _yukleniyor;

        public BildirimListeGorunumModel(BildirimService bildirimService, RandevuService randevuService)
        {
            _bildirimService = bildirimService;
            _randevuService = randevuService;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            Bildirimler.Clear();
            _sayfaNo = 1;
            DahaFazlaVar = true;
            await BildirimleriEkleAsync();
        }

        [RelayCommand]
        async Task DahaFazlaYukleAsync()
        {
            await BildirimleriEkleAsync();
        }

        [RelayCommand]
        async Task BildirimTapAsync(BildirimGorunumModel gorunum)
        {
            if (!gorunum.Okundu)
            {
                await _bildirimService.OkunduIsaretle(gorunum.Bildirim.BildirimId);
                gorunum.IsaretleOkundu();
                AltBaslikGuncelle();
            }

            if (gorunum.Bildirim.RandevuId.HasValue)
            {
                var vm = RandevuDetayGorunumModel.FromId(_randevuService, gorunum.Bildirim.RandevuId.Value);
                await Shell.Current.Navigation.PushAsync(new RandevuDetayView(vm));
            }
        }

        [RelayCommand]
        async Task TumunuOkunduAsync()
        {
            var sonuc = await _bildirimService.TumunuOkunduIsaretle();
            if (!sonuc) return;
            foreach (var b in Bildirimler)
                b.IsaretleOkundu();
            AltBaslik = "Tüm bildirimler okundu";
        }

        private async Task BildirimleriEkleAsync()
        {
            if (_yukleniyor || !DahaFazlaVar) return;
            _yukleniyor = true;
            try
            {
                var yeniler = await _bildirimService.BildirimleriGetir(_sayfaNo);
                if (yeniler.Count == 0)
                {
                    DahaFazlaVar = false;
                    return;
                }
                foreach (var b in yeniler)
                    Bildirimler.Add(new BildirimGorunumModel(b));
                _sayfaNo++;
                AltBaslikGuncelle();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BILDIRIM LISTE HATASI]: {ex.Message}");
                await Application.Current!.MainPage!.DisplayAlert("Hata", $"Bildirimler yüklenemedi.\n{ex.Message}", "Tamam");
            }
            finally
            {
                _yukleniyor = false;
            }
        }

        private void AltBaslikGuncelle()
        {
            var okunmamis = Bildirimler.Count(b => b.OkunmadiMi);
            AltBaslik = okunmamis > 0 ? $"{okunmamis} okunmamış bildirim" : "Tüm bildirimler okundu";
        }
    }
}
