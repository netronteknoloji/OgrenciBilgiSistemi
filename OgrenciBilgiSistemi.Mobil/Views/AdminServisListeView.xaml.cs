using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminServisListeView : ContentPage
    {
        public AdminServisListeView(AdminServisListeGorunumModel gorunumModel)
        {
            InitializeComponent();
            BindingContext = gorunumModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminServisListeGorunumModel vm)
                vm.YukleCommand.Execute(null);
        }
    }
}
