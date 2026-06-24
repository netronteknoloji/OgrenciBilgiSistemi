using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class GirisView : ContentPage
    {
        private readonly GirisGorunumModel _vm;

        public GirisView(GirisGorunumModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            _vm = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _vm.YukleCommand.Execute(null);
        }

        // SelectionChanged olayı burada kalıyor — seçim sonrası şifre alanına focus vermek için
        private void OnOneriSecildi(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is string secilen)
            {
                _vm.OneriSecCommand.Execute(secilen);
                TxtPassword.Focus();
                if (sender is CollectionView cv) cv.SelectedItem = null;
            }
        }
    }
}
