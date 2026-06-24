using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminYemekhaneBugunView : ContentPage
    {
        public AdminYemekhaneBugunView(AdminYemekhaneBugunGorunumModel gorunumModel)
        {
            InitializeComponent();
            BindingContext = gorunumModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminYemekhaneBugunGorunumModel vm)
                vm.YukleCommand.Execute(null);
        }
    }
}
