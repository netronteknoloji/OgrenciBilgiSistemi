using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminOgretmenListeView : ContentPage
    {
        public AdminOgretmenListeView(AdminOgretmenListeGorunumModel gorunumModel)
        {
            InitializeComponent();
            BindingContext = gorunumModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminOgretmenListeGorunumModel vm)
                vm.YukleCommand.Execute(null);
        }
    }
}
