using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class RandevuOlusturView : ContentPage
    {
        private readonly RandevuOlusturGorunumModel _vm;

        public RandevuOlusturView(RandevuOlusturGorunumModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            _vm = vm;
            TarihSecici.MinimumDate = DateTime.Today;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _vm.YukleCommand.Execute(null);
        }

        private void OnVeliOgrenciSecildi(object sender, SelectionChangedEventArgs e)
        {
            _vm.OgrenciSec(e.CurrentSelection.FirstOrDefault() as Ogrenci);
        }

        private void OnSlotSecildi(object sender, SelectionChangedEventArgs e)
        {
            _vm.SlotSec(e.CurrentSelection.FirstOrDefault() as RandevuSlot);
        }
    }
}
