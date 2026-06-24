using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class OgretmenRandevuYonetimView : ContentPage
    {
        private readonly OgretmenRandevuYonetimGorunumModel _vm;

        public OgretmenRandevuYonetimView(OgretmenRandevuYonetimGorunumModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            _vm = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _vm.YukleCommand.Execute(null);
        }
    }
}
