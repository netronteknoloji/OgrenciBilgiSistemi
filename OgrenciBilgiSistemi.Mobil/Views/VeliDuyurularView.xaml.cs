using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class VeliDuyurularView : ContentPage
    {
        private readonly VeliDuyurularGorunumModel _vm;

        public VeliDuyurularView(VeliDuyurularGorunumModel vm)
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
