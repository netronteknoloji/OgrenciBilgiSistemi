using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class ServisEkraniView : ContentPage
    {
        private readonly ServisEkraniGorunumModel _vm;

        public ServisEkraniView(ServisEkraniGorunumModel vm)
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

        private void OnServisDurumTapped(object sender, TappedEventArgs e)
        {
            if (sender is Border border && border.BindingContext is OgrenciGorunumModel vm && e.Parameter != null)
            {
                if (int.TryParse(e.Parameter.ToString(), out int durumId))
                    vm.ServisDurumId = durumId;
            }
        }
    }
}
