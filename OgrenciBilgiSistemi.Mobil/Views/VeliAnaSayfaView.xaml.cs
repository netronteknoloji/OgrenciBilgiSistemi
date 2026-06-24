using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class VeliAnaSayfaView : ContentPage
    {
        private readonly VeliAnaSayfaGorunumModel _vm;

        public VeliAnaSayfaView(VeliAnaSayfaGorunumModel vm)
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
