using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class RandevuOlusturView : ContentPage
    {
        private readonly RandevuService _randevuService;
        private readonly OgretmenRandevuService _ogretmenRandevuService;
        private readonly VeliService _veliService;
        private readonly OgrenciService _ogrenciService;
        private readonly OgretmenListeService _ogretmenListeService;

        private List<Ogrenci> _cocuklar = new();
        private System.Collections.ObjectModel.ObservableCollection<OgrenciGrubu> _ogrenciGruplari = new();
        private List<OgrenciGrubu> _kendiSinifGrubu = new();
        private List<OgrenciGrubu> _digerSiniflarGruplari = new();
        private Ogrenci? _seciliOgrenci;
        private List<OgretmenBilgi> _ogretmenler = new();
        private List<RandevuSlot> _randevuSlotlar = new();
        private RandevuSlot _secilenSlot;
        private int? _karsiTarafId;
        private int? _sinifOgretmenId;

        public RandevuOlusturView()
        {
            InitializeComponent();

            _randevuService = Application.Current.MainPage.Handler.MauiContext.Services.GetService<RandevuService>();
            _ogretmenRandevuService = Application.Current.MainPage.Handler.MauiContext.Services.GetService<OgretmenRandevuService>();
            _veliService = Application.Current.MainPage.Handler.MauiContext.Services.GetService<VeliService>();
            _ogrenciService = Application.Current.MainPage.Handler.MauiContext.Services.GetService<OgrenciService>();
            _ogretmenListeService = Application.Current.MainPage.Handler.MauiContext.Services.GetService<OgretmenListeService>();

            TarihSecici.MinimumDate = DateTime.Today;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (KullaniciOturum.VeliMi)
            {
                AltBaslikLabel.Text = "Öğretmenle görüşme talebi";
                OgretmenSecimBorder.IsVisible = true;
                SlotSecimBorder.IsVisible = true;
                await VeliIcinHazirla();
            }
            else if (KullaniciOturum.OgretmenMi)
            {
                AltBaslikLabel.Text = "Veli ile görüşme planla";
                VeliSecimBorder.IsVisible = true;
                TarihSecimBorder.IsVisible = true;
                await OgretmenIcinHazirla();
            }
        }

        private async Task VeliIcinHazirla()
        {
            try
            {
                _cocuklar = await _veliService.CocuklarimiGetir();

                if (_cocuklar.Count == 1)
                {
                    OgrenciPicker.ItemsSource = _cocuklar.Select(c => c.OgrenciAdSoyad).ToList();
                    OgrenciPicker.SelectedIndex = 0;
                    OgrenciPicker.IsEnabled = false;
                }
                else
                {
                    OgrenciPicker.ItemsSource = _cocuklar.Select(c => c.OgrenciAdSoyad).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RANDEVU FORM HATASI]: {ex.Message}");
            }
        }

        private async Task OgretmenIcinHazirla()
        {
            try
            {
                var tumOgrenciler = await _ogrenciService.TumOgrencileriGetirAsync();
                var ogretmenBirimId = KullaniciOturum.BirimId;

                _kendiSinifGrubu = new List<OgrenciGrubu>();
                if (ogretmenBirimId.HasValue)
                {
                    var kendiSinifOgrencileri = tumOgrenciler
                        .Where(o => o.BirimId == ogretmenBirimId.Value)
                        .OrderBy(o => o.OgrenciAdSoyad)
                        .ToList();

                    if (kendiSinifOgrencileri.Count > 0)
                    {
                        var sinifAdi = kendiSinifOgrencileri[0].SinifAdi ?? "Sınıfım";
                        _kendiSinifGrubu.Add(new OgrenciGrubu($"★ Benim Sınıfım: {sinifAdi}", true, kendiSinifOgrencileri));
                    }
                }

                _digerSiniflarGruplari = tumOgrenciler
                    .Where(o => !ogretmenBirimId.HasValue || o.BirimId != ogretmenBirimId.Value)
                    .GroupBy(o => o.SinifAdi ?? "(Sınıfsız)")
                    .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
                    .Select(g => new OgrenciGrubu(g.Key, false, g.OrderBy(o => o.OgrenciAdSoyad)))
                    .ToList();

                BenimSinifimToggle.IsVisible = _kendiSinifGrubu.Count > 0;
                BenimSinifimCheckBox.IsChecked = _kendiSinifGrubu.Count > 0;

                ListeyiUygula();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RANDEVU FORM HATASI]: {ex.Message}");
            }
        }

        private void ListeyiUygula(bool secimTemizle = true)
        {
            var kaynak = BenimSinifimCheckBox.IsChecked ? _kendiSinifGrubu : _digerSiniflarGruplari;
            var arama = (OgrenciAramaBar?.Text ?? "").Trim();

            List<OgrenciGrubu> hedef;
            if (string.IsNullOrEmpty(arama))
            {
                hedef = kaynak;
            }
            else
            {
                hedef = kaynak
                    .Select(g => new
                    {
                        Grup = g,
                        Eslesen = g.Where(o =>
                            (o.OgrenciAdSoyad ?? "").Contains(arama, StringComparison.CurrentCultureIgnoreCase) ||
                            (o.SinifAdi ?? "").Contains(arama, StringComparison.CurrentCultureIgnoreCase))
                          .ToList()
                    })
                    .Where(x => x.Eslesen.Count > 0)
                    .Select(x => new OgrenciGrubu(x.Grup.BaslikAdi, x.Grup.KendiSinifi, x.Eslesen))
                    .ToList();
            }

            _ogrenciGruplari = new System.Collections.ObjectModel.ObservableCollection<OgrenciGrubu>(hedef);
            VeliOgrenciCollectionView.ItemsSource = _ogrenciGruplari;

            if (secimTemizle)
            {
                _seciliOgrenci = null;
                SeciliOgrenciLabel.IsVisible = false;
                VeliOgrenciCollectionView.SelectedItem = null;
            }
        }

        private void OnBenimSinifimDegisti(object? sender, CheckedChangedEventArgs e)
        {
            ListeyiUygula();
        }

        private void OnOgrenciAramaDegisti(object? sender, TextChangedEventArgs e)
        {
            ListeyiUygula(secimTemizle: false);
        }

        private void OnVeliOgrenciSecildi(object sender, SelectionChangedEventArgs e)
        {
            _seciliOgrenci = e.CurrentSelection.FirstOrDefault() as Ogrenci;
            if (_seciliOgrenci != null)
            {
                var sinifBilgisi = string.IsNullOrEmpty(_seciliOgrenci.SinifAdi) ? "" : $" ({_seciliOgrenci.SinifAdi})";
                SeciliOgrenciLabel.Text = $"Seçili: {_seciliOgrenci.OgrenciAdSoyad}{sinifBilgisi}";
                SeciliOgrenciLabel.IsVisible = true;
            }
            else
            {
                SeciliOgrenciLabel.IsVisible = false;
            }
        }

        private async void OnOgrenciSecildi(object sender, EventArgs e)
        {
            var index = OgrenciPicker.SelectedIndex;
            if (index < 0 || index >= _cocuklar.Count) return;

            try
            {
                var ogrenci = _cocuklar[index];
                _sinifOgretmenId = ogrenci.OgretmenId;

                var tumOgretmenler = await _ogretmenListeService.AktifOgretmenleriGetir();

                if (_sinifOgretmenId.HasValue && _sinifOgretmenId.Value > 0)
                {
                    var sinifOgretmeni = tumOgretmenler.FirstOrDefault(o => o.KullaniciId == _sinifOgretmenId.Value);
                    var digerler = tumOgretmenler.Where(o => o.KullaniciId != _sinifOgretmenId.Value).ToList();
                    _ogretmenler = sinifOgretmeni != null
                        ? new List<OgretmenBilgi> { sinifOgretmeni }.Concat(digerler).ToList()
                        : tumOgretmenler;
                }
                else
                {
                    _ogretmenler = tumOgretmenler;
                }

                OgretmenPicker.ItemsSource = _ogretmenler.Select(o => o.KullaniciAdi).ToList();
                OgretmenPickerBorder.IsVisible = true;

                if (_sinifOgretmenId.HasValue && _sinifOgretmenId.Value > 0 &&
                    _ogretmenler.Count > 0 && _ogretmenler[0].KullaniciId == _sinifOgretmenId.Value)
                {
                    OgretmenPicker.SelectedIndex = 0;
                    OgretmenBilgiLabel.Text = "Sınıf öğretmeni seçili";
                    OgretmenBilgiLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OGRETMEN LISTE HATASI]: {ex.Message}");
            }
        }

        private async void OnOgretmenSecildi(object sender, EventArgs e)
        {
            var index = OgretmenPicker.SelectedIndex;
            if (index < 0 || index >= _ogretmenler.Count) return;

            try
            {
                var secilen = _ogretmenler[index];
                _karsiTarafId = secilen.KullaniciId;
                _secilenSlot = null;

                if (secilen.KullaniciId == _sinifOgretmenId)
                {
                    OgretmenBilgiLabel.Text = "Sınıf öğretmeni seçili";
                    OgretmenBilgiLabel.IsVisible = true;
                }
                else
                {
                    OgretmenBilgiLabel.IsVisible = false;
                }

                _randevuSlotlar = await _ogretmenRandevuService.RandevuSlotlariGetir(secilen.KullaniciId);
                SlotCollection.ItemsSource = _randevuSlotlar;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SLOT YUKLEME HATASI]: {ex.Message}");
            }
        }

        private void OnSlotSecildi(object sender, SelectionChangedEventArgs e)
        {
            _secilenSlot = e.CurrentSelection.FirstOrDefault() as RandevuSlot;
        }

        private async void OnOlusturClicked(object sender, EventArgs e)
        {
            OlusturButton.IsEnabled = false;

            try
            {
                int? ogrenciId = null;
                DateTime randevuTarihi;
                int sureDakika;

                if (KullaniciOturum.VeliMi)
                {
                    if (_karsiTarafId == null)
                    {
                        await DisplayAlert("Uyarı", "Lütfen bir öğretmen seçin.", "Tamam");
                        return;
                    }
                    if (_secilenSlot == null)
                    {
                        await DisplayAlert("Uyarı", "Lütfen bir randevu saati seçin.", "Tamam");
                        return;
                    }

                    var index = OgrenciPicker.SelectedIndex;
                    if (index >= 0 && index < _cocuklar.Count)
                        ogrenciId = _cocuklar[index].OgrenciId;

                    randevuTarihi = _secilenSlot.Tarih.Date +
                        TimeSpan.Parse(_secilenSlot.BaslangicSaati);
                    sureDakika = (int)(TimeSpan.Parse(_secilenSlot.BitisSaati) -
                        TimeSpan.Parse(_secilenSlot.BaslangicSaati)).TotalMinutes;
                }
                else
                {
                    if (_seciliOgrenci is null)
                    {
                        await DisplayAlert("Uyarı", "Lütfen bir öğrenci seçin.", "Tamam");
                        return;
                    }

                    _karsiTarafId = _seciliOgrenci.VeliId;
                    ogrenciId = _seciliOgrenci.OgrenciId;
                    randevuTarihi = TarihSecici.Date + SaatSecici.Time;
                    sureDakika = 30;
                }

                string not = string.IsNullOrWhiteSpace(NotEditor.Text) ? null : NotEditor.Text.Trim();

                var (basarili, hata) = await _randevuService.RandevuOlustur(
                    _karsiTarafId.Value, ogrenciId, randevuTarihi, sureDakika, not);

                if (basarili)
                {
                    await DisplayAlert("Başarılı", "Randevu oluşturuldu.", "Tamam");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Hata", hata ?? "Randevu oluşturulurken bir sorun oluştu.", "Tamam");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RANDEVU OLUSTUR HATASI]: {ex.Message}");
                await DisplayAlert("Hata", "Beklenmeyen bir hata oluştu.", "Tamam");
            }
            finally
            {
                OlusturButton.IsEnabled = true;
            }
        }

    }
}
