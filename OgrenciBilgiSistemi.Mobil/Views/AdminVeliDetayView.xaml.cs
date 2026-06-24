using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminVeliDetayView : ContentPage
    {
        public AdminVeliDetayView(AdminVeliDetayGorunumModel gorunumModel)
        {
            InitializeComponent();
            BindingContext = gorunumModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminVeliDetayGorunumModel vm)
                vm.YukleCommand.Execute(null);
        }
    }
}
