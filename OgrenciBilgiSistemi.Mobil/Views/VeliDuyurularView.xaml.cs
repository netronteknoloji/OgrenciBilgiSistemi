using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class VeliDuyurularView : ContentPage
    {
        private readonly DuyuruService _duyuruService;

        public VeliDuyurularView(DuyuruService duyuruService)
        {
            InitializeComponent();
            _duyuruService = duyuruService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await DuyurulariYukle();
        }

        private async Task DuyurulariYukle()
        {
            try
            {
                var liste = await _duyuruService.BenimDuyurular();
                DuyuruCollection.ItemsSource = liste;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DUYURU LISTE HATASI]: {ex.Message}");
            }
        }
    }
}
