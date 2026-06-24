using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminOgretmenDetayView : ContentPage
    {
        public AdminOgretmenDetayView(AdminOgretmenDetayGorunumModel gorunumModel)
        {
            InitializeComponent();
            BindingContext = gorunumModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminOgretmenDetayGorunumModel vm)
                vm.YukleCommand.Execute(null);
        }
    }
}
