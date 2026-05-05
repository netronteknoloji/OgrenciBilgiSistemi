using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminServisListeView : ContentPage
    {
        private readonly AdminService _adminService;

        public AdminServisListeView(AdminService adminService)
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
                var liste = await _adminService.ServisListesiGetir();

                if (liste is null)
                {
                    ServisCollection.ItemsSource = null;
                    AltBaslikLabel.Text = "";
                    BosDurumLabel.Text = "Liste alınamadı. Sunucuya ulaşılamıyor olabilir.";
                    return;
                }

                ServisCollection.ItemsSource = liste;
                AltBaslikLabel.Text = $"{liste.Count} servis";

                if (liste.Count == 0)
                    BosDurumLabel.Text = "Kayıtlı servis bulunamadı.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminServisListe Yükleme Hatası: {ex.Message}");
                BosDurumLabel.Text = "Veriler yüklenemedi.";
            }
        }

        private async void OnServisSecildi(object sender, TappedEventArgs e)
        {
            if ((sender as Border)?.BindingContext is ServisListeOgesi servis)
                await Navigation.PushAsync(new AdminServisDetayView(servis, _adminService));
        }
    }
}
