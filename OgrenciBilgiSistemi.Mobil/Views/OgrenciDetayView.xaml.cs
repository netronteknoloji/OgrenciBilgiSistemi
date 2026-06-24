using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.ViewModels;
using OgrenciBilgiSistemi.Shared.Constants;
using Microsoft.Maui.Controls.Shapes;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class OgrenciDetayView : ContentPage
    {
        private readonly OgrenciDetayGorunumModel _vm;

        public OgrenciDetayView(int studentId, OgrenciService? ogrenciService = null, ServisService? servisService = null)
        {
            InitializeComponent();
            var ogrenci = ogrenciService ?? IPlatformApplication.Current.Services.GetRequiredService<OgrenciService>();
            var servis = servisService ?? IPlatformApplication.Current.Services.GetRequiredService<ServisService>();
            _vm = new OgrenciDetayGorunumModel(studentId, ogrenci, servis);
            BindingContext = _vm;
            _vm.PropertyChanged += OnVmPropertyChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _vm.YukleCommand.Execute(null);
        }

        private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OgrenciDetayGorunumModel.YoklamaHucreler))
                RenderYoklamaMatrisi();
            else if (e.PropertyName == nameof(OgrenciDetayGorunumModel.GirisCikisKayitlari)
                  || e.PropertyName == nameof(OgrenciDetayGorunumModel.YemekhaneKayitlari))
                RenderGirisCikis();
            else if (e.PropertyName == nameof(OgrenciDetayGorunumModel.ServisYoklamaKayitlari))
                RenderServisYoklama();
        }

        private void RenderYoklamaMatrisi()
        {
            var cellsToRemove = GridAttendanceMatrix.Children
                .Cast<View>()
                .Where(c => Grid.GetRow(c) > 0 && Grid.GetColumn(c) >= 1 && Grid.GetColumn(c) <= 5)
                .ToList();
            foreach (var cell in cellsToRemove)
                GridAttendanceMatrix.Children.Remove(cell);

            foreach (var hucre in _vm.YoklamaHucreler)
            {
                var box = new Border
                {
                    BackgroundColor = Color.FromArgb(hucre.RenkHex),
                    StrokeShape = new RoundRectangle { CornerRadius = 4 },
                    Margin = new Thickness(1),
                    HeightRequest = 25,
                    WidthRequest = 25
                };
                GridAttendanceMatrix.Add(box, hucre.GunIndex + 1, hucre.DersIndex);
            }
        }

        private void RenderGirisCikis()
        {
            LayoutGirisCikis.Children.Clear();
            if (_vm.GirisCikisKayitlari.Count == 0)
            {
                LayoutGirisCikis.Children.Add(new Label
                {
                    Text = "Bu hafta giriş/çıkış kaydı bulunmuyor.",
                    FontSize = 12, TextColor = Color.FromArgb("#BDC3C7"), HorizontalOptions = LayoutOptions.Center
                });
            }
            else
            {
                foreach (var gun in _vm.GirisCikisKayitlari)
                {
                    LayoutGirisCikis.Children.Add(new Label
                    {
                        Text = $"{OgrenciDetayGorunumModel.GunAdi(gun.Gun)}, {gun.Gun:dd.MM.yyyy}",
                        FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C3E50")
                    });

                    foreach (var kayit in gun.Kayitlar)
                    {
                        var satir = new HorizontalStackLayout { Spacing = 10, Margin = new Thickness(10, 2, 0, 0) };
                        if (kayit.Giris.HasValue)
                            satir.Children.Add(new Label
                            {
                                Text = $"Giriş: {kayit.Giris.Value:HH:mm}",
                                FontSize = 13, TextColor = Color.FromArgb(YoklamaRenkleri.GirisHex), FontAttributes = FontAttributes.Bold
                            });
                        if (kayit.Cikis.HasValue)
                            satir.Children.Add(new Label
                            {
                                Text = $"Çıkış: {kayit.Cikis.Value:HH:mm}",
                                FontSize = 13, TextColor = Color.FromArgb(YoklamaRenkleri.CikisHex), FontAttributes = FontAttributes.Bold
                            });
                        LayoutGirisCikis.Children.Add(satir);
                    }
                }
            }

            LayoutYemekhane.Children.Clear();
            if (_vm.YemekhaneKayitlari.Count == 0)
            {
                LayoutYemekhane.Children.Add(new Label
                {
                    Text = "Bu hafta yemekhane kaydı bulunmuyor.",
                    FontSize = 12, TextColor = Color.FromArgb("#BDC3C7"), HorizontalOptions = LayoutOptions.Center
                });
            }
            else
            {
                foreach (var gun in _vm.YemekhaneKayitlari)
                {
                    var satir = new HorizontalStackLayout { Spacing = 10 };
                    satir.Children.Add(new Label
                    {
                        Text = $"{OgrenciDetayGorunumModel.GunAdi(gun.Gun)}, {gun.Gun:dd.MM.yyyy}",
                        FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C3E50")
                    });
                    if (gun.IlkGiris.HasValue)
                        satir.Children.Add(new Label
                        {
                            Text = $"— {gun.IlkGiris.Value:HH:mm}",
                            FontSize = 13, TextColor = Color.FromArgb("#E67E22"), FontAttributes = FontAttributes.Bold
                        });
                    LayoutYemekhane.Children.Add(satir);
                }
            }
        }

        private void RenderServisYoklama()
        {
            LayoutServisYoklama.Children.Clear();
            if (_vm.ServisYoklamaKayitlari.Count == 0)
            {
                LayoutServisYoklama.Children.Add(new Label
                {
                    Text = "Bu hafta servis yoklama kaydı bulunmuyor.",
                    FontSize = 12, TextColor = Color.FromArgb("#BDC3C7"), HorizontalOptions = LayoutOptions.Center
                });
                return;
            }

            foreach (var gun in _vm.ServisYoklamaKayitlari)
            {
                LayoutServisYoklama.Children.Add(new Label
                {
                    Text = $"{OgrenciDetayGorunumModel.GunAdi(gun.Gun)}, {gun.Gun:dd.MM.yyyy}",
                    FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C3E50")
                });

                var satir = new HorizontalStackLayout { Spacing = 12, Margin = new Thickness(10, 2, 0, 0) };
                satir.Children.Add(YoklamaEtiketi("Sabah", gun.SabahVar, gun.SabahDurum));
                satir.Children.Add(YoklamaEtiketi("Akşam", gun.AksamVar, gun.AksamDurum));
                LayoutServisYoklama.Children.Add(satir);
            }
        }

        private static Label YoklamaEtiketi(string periyotAdi, bool kayitVar, int durumId)
        {
            string isaret;
            string renk;
            if (!kayitVar) { isaret = "—"; renk = "#BDC3C7"; }
            else if (durumId == 1) { isaret = "✓"; renk = YoklamaRenkleri.GirisHex; }
            else { isaret = "✗"; renk = YoklamaRenkleri.CikisHex; }

            return new Label
            {
                Text = $"{periyotAdi} {isaret}",
                FontSize = 13,
                TextColor = Color.FromArgb(renk),
                FontAttributes = FontAttributes.Bold
            };
        }

        private void OnPhoneTapped(object sender, EventArgs e)
        {
            try
            {
                string? phoneNumber = LblParentPhone.Text?.Trim();
                if (PhoneDialer.Default.IsSupported && !string.IsNullOrEmpty(phoneNumber) && phoneNumber != "-")
                    PhoneDialer.Default.Open(phoneNumber);
            }
            catch { }
        }
    }
}
