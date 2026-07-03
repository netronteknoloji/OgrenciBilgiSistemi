using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Views;

public partial class PushTaniView : ContentPage
{
    private readonly PushTaniGorunumModel _vm;

    public PushTaniView(PushTaniGorunumModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.YenileCommand.Execute(null);
    }
}
