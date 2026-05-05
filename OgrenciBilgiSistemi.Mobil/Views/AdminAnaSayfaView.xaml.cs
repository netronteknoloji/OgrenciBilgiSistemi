using System.Collections.ObjectModel;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminAnaSayfaView : ContentPage
    {
        private const int OgrenciOnizlemeSayisi = 20;

        private readonly AdminService _adminService;
        private readonly OgrenciService _ogrenciService;

        public ObservableCollection<ServisListeOgesi> Servisler { get; } = new();
        public ObservableCollection<Ogrenci> OgrencilerOnizleme { get; } = new();

        public AdminAnaSayfaView(AdminService adminService, OgrenciService ogrenciService)
        {
            try
            {
                InitializeComponent();
                _adminService = adminService;
                _ogrenciService = ogrenciService;
                BindingContext = this;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminAnaSayfaView Init Hatası: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            WelcomeLabel.Text = $"Merhaba, {KullaniciOturum.AdSoyad}";
            await VerileriYukle();
        }

        private async void OnYenile(object sender, EventArgs e)
        {
            try
            {
                await VerileriYukle();
            }
            finally
            {
                AnaRefreshView.IsRefreshing = false;
            }
        }

        private async Task VerileriYukle()
        {
            try
            {
                var servisGorevi = _adminService.ServisListesiGetir();
                var ogrenciGorevi = _ogrenciService.TumOgrencileriGetirAsync();
                var yemekhaneGorevi = _adminService.YemekhaneBugunGetir();
                var anakapiGorevi = _adminService.AnakapiCikisBugunGetir();
                await Task.WhenAll(servisGorevi, ogrenciGorevi, yemekhaneGorevi, anakapiGorevi);

                var servisler = await servisGorevi;
                var ogrenciler = await ogrenciGorevi;
                var yemekhane = await yemekhaneGorevi;
                var anakapi = await anakapiGorevi;

                Servisler.Clear();
                foreach (var s in servisler)
                    Servisler.Add(s);

                OgrencilerOnizleme.Clear();
                foreach (var o in ogrenciler.Take(OgrenciOnizlemeSayisi))
                    OgrencilerOnizleme.Add(o);

                ToplamServisLabel.Text = servisler.Count.ToString();
                YemekhaneSayisiLabel.Text = yemekhane.Count.ToString();
                AnakapiCikisSayisiLabel.Text = anakapi.Count.ToString();
                ServisSayisiAltLabel.Text = $"{servisler.Count} kayıt";
                ServisBosLabel.IsVisible = servisler.Count == 0;
                OgrenciBosLabel.IsVisible = ogrenciler.Count == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminAnaSayfa Yükleme Hatası: {ex.Message}");
                await DisplayAlert("Hata", "Veriler yüklenemedi.", "Tamam");
            }
        }

        private async void OnYemekhaneTapped(object sender, TappedEventArgs e)
            => await Navigation.PushAsync(new AdminYemekhaneBugunView(_adminService));

        private async void OnAnakapiCikisTapped(object sender, TappedEventArgs e)
            => await Navigation.PushAsync(new AdminAnakapiCikisBugunView(_adminService));

        private async void OnTumOgrencileriGorTapped(object sender, TappedEventArgs e)
        {
            var service = Servis<OgrenciService>();
            if (service is null) return;
            await Navigation.PushAsync(new AdminOgrenciListeView(service));
        }

        private async void OnServisSecildi(object sender, TappedEventArgs e)
        {
            if ((sender as Border)?.BindingContext is ServisListeOgesi servis)
            {
                var servisService = Servis<ServisService>();
                if (servisService is null) return;
                await Navigation.PushAsync(new AdminServisDetayView(servis, servisService));
            }
        }

        private async void OnVelilerTapped(object sender, TappedEventArgs e)
        {
            var service = Servis<VeliListeService>();
            if (service is null) return;
            await Navigation.PushAsync(new AdminVeliListeView(service));
        }

        private async void OnOgretmenlerTapped(object sender, TappedEventArgs e)
        {
            var service = Servis<OgretmenListeService>();
            if (service is null) return;
            await Navigation.PushAsync(new AdminOgretmenListeView(service));
        }

        private async void OnSiniflarTapped(object sender, TappedEventArgs e)
        {
            var service = Servis<SinifService>();
            if (service is null) return;
            await Navigation.PushAsync(new AdminSinifListeView(service));
        }

        private static T? Servis<T>() where T : class
            => Application.Current?.MainPage?.Handler?.MauiContext?.Services.GetService<T>();
    }
}
