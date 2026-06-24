using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminVeliListeView : ContentPage
    {
        public AdminVeliListeView(AdminVeliListeGorunumModel gorunumModel)
        {
            InitializeComponent();
            BindingContext = gorunumModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminVeliListeGorunumModel vm)
                vm.YukleCommand.Execute(null);
        }
    }
}
