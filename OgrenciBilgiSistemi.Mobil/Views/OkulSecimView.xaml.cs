using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class OkulSecimView : ContentPage
    {
        private readonly OkulSecimGorunumModel _vm;

        public OkulSecimView(OkulSecimGorunumModel vm)
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

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _vm.Temizle();
        }
    }
}
