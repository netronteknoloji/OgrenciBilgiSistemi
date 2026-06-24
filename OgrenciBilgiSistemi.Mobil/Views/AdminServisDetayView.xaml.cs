using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminServisDetayView : ContentPage
    {
        public AdminServisDetayView(AdminServisDetayGorunumModel gorunumModel)
        {
            InitializeComponent();
            BindingContext = gorunumModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminServisDetayGorunumModel vm)
                vm.YukleCommand.Execute(null);
        }
    }
}
