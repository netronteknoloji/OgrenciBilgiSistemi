using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminSinifOgrenciListeView : ContentPage
    {
        public AdminSinifOgrenciListeView(AdminSinifOgrenciListeGorunumModel gorunumModel)
        {
            InitializeComponent();
            BindingContext = gorunumModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminSinifOgrenciListeGorunumModel vm)
                vm.YukleCommand.Execute(null);
        }
    }
}
