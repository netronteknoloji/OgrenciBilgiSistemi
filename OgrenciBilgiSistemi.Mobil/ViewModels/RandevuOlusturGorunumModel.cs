using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using System.Collections.ObjectModel;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class RandevuOlusturGorunumModel : ObservableObject
    {
        private readonly RandevuService _randevuService;
        private readonly OgretmenRandevuService _ogretmenRandevuService;
        private readonly VeliService _veliService;
        private readonly OgrenciService _ogrenciService;
        private readonly OgretmenListeService _ogretmenListeService;

        private List<Ogrenci> _cocuklar = [];
        private List<OgretmenBilgi> _ogretmenler = [];
        private List<OgrenciGrubu> _kendiSinifGrubu = [];
        private List<OgrenciGrubu> _digerSiniflarGrubu = [];
        private Ogrenci? _seciliOgrenci;
        private RandevuSlot? _secilenSlot;
        private int? _karsiTarafId;
        private int? _sinifOgretmenId;

        // Rol paylaşımlı ekran: giren rolün tema rengiyle boyanır (rol login sonrası değişmez)
        public Color TemaRenk => RolTema.VurguRenk;
        public Brush TemaGradyan => RolTema.BaslikGradyan;

        [ObservableProperty] private bool veliModuGorunur;
        [ObservableProperty] private bool ogretmenModuGorunur;
        [ObservableProperty] private string altBaslikMetni = string.Empty;

        [ObservableProperty] private IReadOnlyList<string> cocukAdlari = [];
        [ObservableProperty] private int secilenCocukIndex = -1;
        [ObservableProperty] private bool cocukPickerEtkin = true;

        [ObservableProperty] private IReadOnlyList<string> ogretmenAdlari = [];
        [ObservableProperty] private int secilenOgretmenIndex = -1;
        [ObservableProperty] private bool ogretmenPickerGorunur;
        [ObservableProperty] private bool sinifOgretmeniBilgisiGorunur;
        [ObservableProperty] private string sinifOgretmeniBilgisi = "Sınıf öğretmeni seçili";

        [ObservableProperty] private IReadOnlyList<RandevuSlot> slotListesi = [];

        [ObservableProperty] private ObservableCollection<OgrenciGrubu> ogrenciGruplari = [];
        [ObservableProperty] private string seciliOgrenciMetni = string.Empty;
        [ObservableProperty] private bool seciliOgrenciGorunur;
        [ObservableProperty] private bool benimSinifimGorunur;
        [ObservableProperty] private bool benimSinifimSecili;
        [ObservableProperty] private string sinifAramaMetni = string.Empty;

        [ObservableProperty] private DateTime secilenTarih = DateTime.Today;
        [ObservableProperty] private TimeSpan baslangicSaatiSecilen = new(9, 0, 0);
        [ObservableProperty] private TimeSpan bitisSaatiSecilen = new(9, 30, 0);

        [ObservableProperty] private string not = string.Empty;
        [ObservableProperty] private bool olusturButonEtkin = true;

        public RandevuOlusturGorunumModel(
            RandevuService randevuService,
            OgretmenRandevuService ogretmenRandevuService,
            VeliService veliService,
            OgrenciService ogrenciService,
            OgretmenListeService ogretmenListeService)
        {
            _randevuService = randevuService;
            _ogretmenRandevuService = ogretmenRandevuService;
            _veliService = veliService;
            _ogrenciService = ogrenciService;
            _ogretmenListeService = ogretmenListeService;
        }

        partial void OnSecilenCocukIndexChanged(int value) { _ = CocukSecildiAsync(value); }
        partial void OnSecilenOgretmenIndexChanged(int value) { _ = OgretmenSecildiAsync(value); }
        partial void OnSinifAramaMetniChanged(string value) => ListeyiUygula(secimTemizle: false);
        partial void OnBenimSinifimSeciliChanged(bool value) => ListeyiUygula();

        [RelayCommand]
        async Task YukleAsync()
        {
            if (KullaniciOturum.VeliMi)
            {
                AltBaslikMetni = "Öğretmenle görüşme talebi";
                VeliModuGorunur = true;
                await VeliIcinHazirla();
            }
            else if (KullaniciOturum.OgretmenMi)
            {
                AltBaslikMetni = "Veli ile görüşme planla";
                OgretmenModuGorunur = true;
                await OgretmenIcinHazirla();
            }
        }

        private async Task VeliIcinHazirla()
        {
            try
            {
                _cocuklar = await _veliService.CocuklarimiGetir();
                CocukAdlari = _cocuklar.Select(c => c.OgrenciAdSoyad).ToList();
                if (_cocuklar.Count == 1)
                {
                    SecilenCocukIndex = 0;
                    CocukPickerEtkin = false;
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

                _kendiSinifGrubu = [];
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

                _digerSiniflarGrubu = tumOgrenciler
                    .Where(o => !ogretmenBirimId.HasValue || o.BirimId != ogretmenBirimId.Value)
                    .GroupBy(o => o.SinifAdi ?? "(Sınıfsız)")
                    .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
                    .Select(g => new OgrenciGrubu(g.Key, false, g.OrderBy(o => o.OgrenciAdSoyad)))
                    .ToList();

                BenimSinifimGorunur = _kendiSinifGrubu.Count > 0;
                BenimSinifimSecili = _kendiSinifGrubu.Count > 0;
                ListeyiUygula();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RANDEVU FORM HATASI]: {ex.Message}");
            }
        }

        private void ListeyiUygula(bool secimTemizle = true)
        {
            var arama = SinifAramaMetni.Trim();
            List<OgrenciGrubu> hedef;

            if (string.IsNullOrEmpty(arama))
            {
                hedef = BenimSinifimSecili ? _kendiSinifGrubu : _digerSiniflarGrubu;
            }
            else
            {
                hedef = _kendiSinifGrubu.Concat(_digerSiniflarGrubu)
                    .Select(g =>
                    {
                        var eslesenler = g
                            .Where(o =>
                                (o.OgrenciAdSoyad ?? "").Contains(arama, StringComparison.CurrentCultureIgnoreCase)
                                || (o.SinifAdi ?? "").Contains(arama, StringComparison.CurrentCultureIgnoreCase))
                            .ToList();
                        return eslesenler.Count > 0 ? new OgrenciGrubu(g.BaslikAdi, g.KendiSinifi, eslesenler) : null;
                    })
                    .Where(g => g != null)
                    .Cast<OgrenciGrubu>()
                    .ToList();
            }

            OgrenciGruplari = new ObservableCollection<OgrenciGrubu>(hedef);

            if (secimTemizle)
            {
                _seciliOgrenci = null;
                SeciliOgrenciGorunur = false;
            }
        }

        public void OgrenciSec(Ogrenci? ogrenci)
        {
            _seciliOgrenci = ogrenci;
            if (ogrenci != null)
            {
                var sinifBilgisi = string.IsNullOrEmpty(ogrenci.SinifAdi) ? "" : $" ({ogrenci.SinifAdi})";
                SeciliOgrenciMetni = $"Seçili: {ogrenci.OgrenciAdSoyad}{sinifBilgisi}";
                SeciliOgrenciGorunur = true;
            }
            else
            {
                SeciliOgrenciGorunur = false;
            }
        }

        public void SlotSec(RandevuSlot? slot) => _secilenSlot = slot;

        private async Task CocukSecildiAsync(int index)
        {
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

                OgretmenAdlari = _ogretmenler.Select(o => o.KullaniciAdi).ToList();
                OgretmenPickerGorunur = true;

                if (_sinifOgretmenId.HasValue && _sinifOgretmenId.Value > 0 &&
                    _ogretmenler.Count > 0 && _ogretmenler[0].KullaniciId == _sinifOgretmenId.Value)
                {
                    SecilenOgretmenIndex = 0;
                    SinifOgretmeniBilgisi = "Sınıf öğretmeni seçili";
                    SinifOgretmeniBilgisiGorunur = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OGRETMEN LISTE HATASI]: {ex.Message}");
            }
        }

        private async Task OgretmenSecildiAsync(int index)
        {
            if (index < 0 || index >= _ogretmenler.Count) return;
            try
            {
                var secilen = _ogretmenler[index];
                _karsiTarafId = secilen.KullaniciId;
                _secilenSlot = null;

                SinifOgretmeniBilgisiGorunur = secilen.KullaniciId == _sinifOgretmenId;

                SlotListesi = await _ogretmenRandevuService.RandevuSlotlariGetir(secilen.KullaniciId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SLOT YUKLEME HATASI]: {ex.Message}");
            }
        }

        [RelayCommand]
        async Task OlusturAsync()
        {
            OlusturButonEtkin = false;
            try
            {
                int? ogrenciId = null;
                DateTime randevuTarihi;
                int sureDakika;

                if (KullaniciOturum.VeliMi)
                {
                    if (_karsiTarafId == null)
                    {
                        await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Lütfen bir öğretmen seçin.", "Tamam");
                        return;
                    }
                    if (_secilenSlot == null)
                    {
                        await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Lütfen bir randevu saati seçin.", "Tamam");
                        return;
                    }
                    if (SecilenCocukIndex >= 0 && SecilenCocukIndex < _cocuklar.Count)
                        ogrenciId = _cocuklar[SecilenCocukIndex].OgrenciId;

                    randevuTarihi = _secilenSlot.Tarih.Date + TimeSpan.Parse(_secilenSlot.BaslangicSaati);
                    sureDakika = (int)(TimeSpan.Parse(_secilenSlot.BitisSaati) - TimeSpan.Parse(_secilenSlot.BaslangicSaati)).TotalMinutes;
                }
                else
                {
                    if (_seciliOgrenci is null)
                    {
                        await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Lütfen bir öğrenci seçin.", "Tamam");
                        return;
                    }
                    if (BitisSaatiSecilen <= BaslangicSaatiSecilen)
                    {
                        await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Bitiş saati başlangıç saatinden sonra olmalı.", "Tamam");
                        return;
                    }

                    _karsiTarafId = _seciliOgrenci.VeliId;
                    ogrenciId = _seciliOgrenci.OgrenciId;
                    randevuTarihi = SecilenTarih.Date + BaslangicSaatiSecilen;
                    sureDakika = (int)(BitisSaatiSecilen - BaslangicSaatiSecilen).TotalMinutes;

                    var (cakismaVar, cakismaMesaji) = await _randevuService.CakismaKontrolu(randevuTarihi, sureDakika, _karsiTarafId);
                    if (cakismaVar)
                    {
                        await Application.Current!.MainPage!.DisplayAlert("Çakışma",
                            cakismaMesaji ?? "Bu zaman aralığında zaten bir randevu bulunmaktadır.", "Tamam");
                        return;
                    }
                }

                string? notMetni = string.IsNullOrWhiteSpace(Not) ? null : Not.Trim();
                var (basarili, hata) = await _randevuService.RandevuOlustur(
                    _karsiTarafId!.Value, ogrenciId, randevuTarihi, sureDakika, notMetni);

                if (basarili)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Randevu oluşturuldu.", "Tamam");
                    await Shell.Current.Navigation.PopAsync();
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Hata", hata ?? "Randevu oluşturulurken bir sorun oluştu.", "Tamam");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RANDEVU OLUSTUR HATASI]: {ex.Message}");
                await Application.Current!.MainPage!.DisplayAlert("Hata", "Beklenmeyen bir hata oluştu.", "Tamam");
            }
            finally
            {
                OlusturButonEtkin = true;
            }
        }
    }
}
