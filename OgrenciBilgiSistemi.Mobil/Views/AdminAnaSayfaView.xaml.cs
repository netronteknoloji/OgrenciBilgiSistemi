using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminAnaSayfaView : ContentPage
    {
        public AdminAnaSayfaView(AdminAnaSayfaGorunumModel gorunumModel)
        {
            InitializeComponent();
            BindingContext = gorunumModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminAnaSayfaGorunumModel vm)
                vm.YukleCommand.Execute(null);
        }
    }
}
