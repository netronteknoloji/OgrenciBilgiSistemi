namespace OgrenciBilgiSistemi.Mobil.Views;

/// <summary>
/// Liste ekranlarının ortak başlık bileşeni: gradyan şerit + başlık + sayaç alt yazısı
/// + opsiyonel üste binen arama kartı. Alt yazı boşluğu (35) ile arama kartının
/// negatif margin'i (-28) birbirine bağlıdır; sayacın örtülmemesi için birlikte değişmelidir.
/// </summary>
public partial class ListeBaslikView : ContentView
{
    #region Bindable Properties
    public static readonly BindableProperty BaslikProperty =
        BindableProperty.Create(nameof(Baslik), typeof(string), typeof(ListeBaslikView), string.Empty);

    public static readonly BindableProperty AltBaslikProperty =
        BindableProperty.Create(nameof(AltBaslik), typeof(string), typeof(ListeBaslikView), string.Empty);

    public static readonly BindableProperty AramaGosterProperty =
        BindableProperty.Create(nameof(AramaGoster), typeof(bool), typeof(ListeBaslikView), false);

    public static readonly BindableProperty AramaMetniProperty =
        BindableProperty.Create(nameof(AramaMetni), typeof(string), typeof(ListeBaslikView), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty AramaPlaceholderProperty =
        BindableProperty.Create(nameof(AramaPlaceholder), typeof(string), typeof(ListeBaslikView), "Ara...");

    // Brush tipinde: statik kaynak (AdminBaslikGradyan) veya RolTema bindingi ({Binding TemaGradyan}) verilebilir.
    public static readonly BindableProperty BaslikGradyanProperty =
        BindableProperty.Create(nameof(BaslikGradyan), typeof(Brush), typeof(ListeBaslikView));

    public static readonly BindableProperty VurguRenkProperty =
        BindableProperty.Create(nameof(VurguRenk), typeof(Color), typeof(ListeBaslikView), Color.FromArgb("#4C6EF5"));

    public static readonly BindableProperty ShowBackProperty =
        BindableProperty.Create(nameof(ShowBack), typeof(bool), typeof(ListeBaslikView), true);

    public static readonly BindableProperty ShowProfileProperty =
        BindableProperty.Create(nameof(ShowProfile), typeof(bool), typeof(ListeBaslikView), false);

    public string Baslik { get => (string)GetValue(BaslikProperty); set => SetValue(BaslikProperty, value); }
    public string AltBaslik { get => (string)GetValue(AltBaslikProperty); set => SetValue(AltBaslikProperty, value); }
    public bool AramaGoster { get => (bool)GetValue(AramaGosterProperty); set => SetValue(AramaGosterProperty, value); }
    public string AramaMetni { get => (string)GetValue(AramaMetniProperty); set => SetValue(AramaMetniProperty, value); }
    public string AramaPlaceholder { get => (string)GetValue(AramaPlaceholderProperty); set => SetValue(AramaPlaceholderProperty, value); }
    public Brush BaslikGradyan { get => (Brush)GetValue(BaslikGradyanProperty); set => SetValue(BaslikGradyanProperty, value); }
    public Color VurguRenk { get => (Color)GetValue(VurguRenkProperty); set => SetValue(VurguRenkProperty, value); }
    public bool ShowBack { get => (bool)GetValue(ShowBackProperty); set => SetValue(ShowBackProperty, value); }
    public bool ShowProfile { get => (bool)GetValue(ShowProfileProperty); set => SetValue(ShowProfileProperty, value); }
    #endregion

    public ListeBaslikView()
    {
        InitializeComponent();
    }
}
