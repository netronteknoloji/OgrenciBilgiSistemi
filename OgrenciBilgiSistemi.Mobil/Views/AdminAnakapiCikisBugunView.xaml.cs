using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminAnakapiCikisBugunView : ContentPage
    {
        public AdminAnakapiCikisBugunView(AdminAnakapiCikisBugunGorunumModel gorunumModel)
        {
            InitializeComponent();
            BindingContext = gorunumModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminAnakapiCikisBugunGorunumModel vm)
                vm.YukleCommand.Execute(null);
        }
    }
}
