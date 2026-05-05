using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminAnakapiCikisBugunView : ContentPage
    {
        private readonly AdminService _adminService;

        public AdminAnakapiCikisBugunView(AdminService adminService)
        {
            InitializeComponent();
            _adminService = adminService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ListeyiYukle();
        }

        private async Task ListeyiYukle()
        {
            try
            {
                var liste = await _adminService.AnakapiCikisBugunGetir();

                if (liste is null)
                {
                    ListeCollection.ItemsSource = null;
                    AltBaslikLabel.Text = "";
                    BosDurumLabel.Text = "Liste alınamadı. Sunucuya ulaşılamıyor olabilir.";
                    return;
                }

                ListeCollection.ItemsSource = liste;
                AltBaslikLabel.Text = $"Bugün {liste.Count} öğrenci";

                if (liste.Count == 0)
                    BosDurumLabel.Text = "Bugün ana kapıdan çıkış yapan öğrenci yok.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminAnakapiCikisBugun Yükleme Hatası: {ex.Message}");
                BosDurumLabel.Text = "Veriler yüklenemedi.";
            }
        }

        private async void OnOgrenciSecildi(object sender, TappedEventArgs e)
        {
            if ((sender as Border)?.BindingContext is AnakapiCikisBugunOgesi ogesi)
            {
                var ogrenciService = Servis<OgrenciService>();
                if (ogrenciService is null) return;
                await Navigation.PushAsync(new OgrenciDetayView(ogesi.OgrenciId, ogrenciService));
            }
        }

        private static T? Servis<T>() where T : class
            => Application.Current?.MainPage?.Handler?.MauiContext?.Services.GetService<T>();
    }
}
