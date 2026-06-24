using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminOgrenciListeView : ContentPage
    {
        public AdminOgrenciListeView(AdminOgrenciListeGorunumModel gorunumModel)
        {
            InitializeComponent();
            BindingContext = gorunumModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminOgrenciListeGorunumModel vm)
                vm.YukleCommand.Execute(null);
        }
    }
}
