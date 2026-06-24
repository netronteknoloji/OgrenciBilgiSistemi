using CommunityToolkit.Mvvm.Messaging;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class BildirimListeView : ContentPage
    {
        private readonly BildirimListeGorunumModel _vm;

        public BildirimListeView(BildirimListeGorunumModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            _vm = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            WeakReferenceMessenger.Default.Register<BildirimGeldiMesaji>(this, (r, m) =>
                MainThread.BeginInvokeOnMainThread(() => _vm.YukleCommand.Execute(null)));
            _vm.YukleCommand.Execute(null);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            WeakReferenceMessenger.Default.Unregister<BildirimGeldiMesaji>(this);
        }
    }
}
