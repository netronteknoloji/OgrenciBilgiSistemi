using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class RandevuDetayView : ContentPage
    {
        public RandevuDetayView(RandevuDetayGorunumModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ((RandevuDetayGorunumModel)BindingContext).YukleCommand.Execute(null);
        }
    }
}
