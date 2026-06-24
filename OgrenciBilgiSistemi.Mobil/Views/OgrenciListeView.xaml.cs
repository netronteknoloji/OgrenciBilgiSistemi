using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class OgrenciListeView : ContentPage
    {
        private readonly OgrenciListeGorunumModel _vm;

        public OgrenciListeView(OgrenciListeGorunumModel vm)
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

        private void OnStatusTapped(object sender, TappedEventArgs e)
        {
            if (sender is Border border && border.BindingContext is OgrenciGorunumModel vm && e.Parameter != null)
            {
                if (int.TryParse(e.Parameter.ToString(), out int statusId))
                    vm.SecilenDurumId = statusId;
            }
        }
    }
}
