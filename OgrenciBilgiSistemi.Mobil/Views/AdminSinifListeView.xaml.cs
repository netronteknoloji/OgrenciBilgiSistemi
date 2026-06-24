using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminSinifListeView : ContentPage
    {
        public AdminSinifListeView(AdminSinifListeGorunumModel gorunumModel)
        {
            InitializeComponent();
            BindingContext = gorunumModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminSinifListeGorunumModel vm)
                vm.YukleCommand.Execute(null);
        }
    }
}
