using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class OgretmenDuyuruOlusturView : ContentPage
    {
        public OgretmenDuyuruOlusturView(OgretmenDuyuruOlusturGorunumModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
