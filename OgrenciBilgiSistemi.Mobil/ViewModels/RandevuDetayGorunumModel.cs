using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class RandevuDetayGorunumModel : ObservableObject
    {
        private readonly RandevuService _randevuService;
        private readonly int _randevuId;
        private bool _veriYuklendi;

        [ObservableProperty] private string durumIkon = string.Empty;
        [ObservableProperty] private string durumAdi = string.Empty;
        [ObservableProperty] private Color durumRenk = Colors.Gray;
        [ObservableProperty] private string durumAciklama = string.Empty;
        [ObservableProperty] private string tarihMetni = string.Empty;
        [ObservableProperty] private string sureMetni = string.Empty;
        [ObservableProperty] private string ogretmenAdi = string.Empty;
        [ObservableProperty] private string veliAdi = string.Empty;
        [ObservableProperty] private string ogrenciAdi = string.Empty;
        [ObservableProperty] private bool ogrenciGorunur;
        [ObservableProperty] private string notMetni = string.Empty;
        [ObservableProperty] private bool notGorunur;
        [ObservableProperty] private bool aksiyonGorunur;
        [ObservableProperty] private bool onaylaGorunur;
        [ObservableProperty] private bool reddetGorunur;
        [ObservableProperty] private bool iptalGorunur;

        private RandevuDetayGorunumModel(RandevuService service, int id, Randevu? randevu = null)
        {
            _randevuService = service;
            _randevuId = id;
            if (randevu != null)
            {
                Goster(randevu);
                _veriYuklendi = true;
            }
        }

        public static RandevuDetayGorunumModel FromRandevu(RandevuService service, Randevu randevu)
            => new(service, randevu.RandevuId, randevu);

        public static RandevuDetayGorunumModel FromId(RandevuService service, int randevuId)
            => new(service, randevuId);

        [RelayCommand]
        async Task YukleAsync()
        {
            if (_veriYuklendi) return;
            try
            {
                var randevu = await _randevuService.RandevuGetir(_randevuId);
                if (randevu != null)
                {
                    Goster(randevu);
                    _veriYuklendi = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RANDEVU DETAY HATASI]: {ex.Message}");
            }
        }

        [RelayCommand]
        async Task OnaylaAsync()
        {
            var onay = await Application.Current!.MainPage!.DisplayAlert("Onay", "Randevuyu onaylamak istiyor musunuz?", "Evet", "Hayır");
            if (!onay) return;
            var sonuc = await _randevuService.Onayla(_randevuId);
            if (sonuc)
            {
                await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Randevu onaylandı.", "Tamam");
                await Shell.Current.Navigation.PopAsync();
            }
            else
                await Application.Current!.MainPage!.DisplayAlert("Hata", "Randevu onaylanırken bir sorun oluştu.", "Tamam");
        }

        [RelayCommand]
        async Task ReddetAsync()
        {
            var onay = await Application.Current!.MainPage!.DisplayAlert("Onay", "Randevuyu reddetmek istiyor musunuz?", "Evet", "Hayır");
            if (!onay) return;
            var sonuc = await _randevuService.Reddet(_randevuId);
            if (sonuc)
            {
                await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Randevu reddedildi.", "Tamam");
                await Shell.Current.Navigation.PopAsync();
            }
            else
                await Application.Current!.MainPage!.DisplayAlert("Hata", "Randevu reddedilirken bir sorun oluştu.", "Tamam");
        }

        [RelayCommand]
        async Task IptalAsync()
        {
            var onay = await Application.Current!.MainPage!.DisplayAlert("Onay", "Randevuyu iptal etmek istiyor musunuz?", "Evet", "Hayır");
            if (!onay) return;
            var sonuc = await _randevuService.IptalEt(_randevuId);
            if (sonuc)
            {
                await Application.Current!.MainPage!.DisplayAlert("Başarılı", "Randevu iptal edildi.", "Tamam");
                await Shell.Current.Navigation.PopAsync();
            }
            else
                await Application.Current!.MainPage!.DisplayAlert("Hata", "Randevu iptal edilirken bir sorun oluştu.", "Tamam");
        }

        private void Goster(Randevu r)
        {
            TarihMetni = r.RandevuTarihi.ToString("dd.MM.yyyy HH:mm");
            SureMetni = $"{r.SureDakika} dakika";
            OgretmenAdi = r.OgretmenAdSoyad;
            VeliAdi = r.VeliAdSoyad;
            OgrenciAdi = r.OgrenciAdSoyad ?? string.Empty;
            OgrenciGorunur = !string.IsNullOrEmpty(r.OgrenciAdSoyad);
            NotMetni = r.Not ?? string.Empty;
            NotGorunur = !string.IsNullOrEmpty(r.Not);

            // Tasarım sistemi semantik renkleri (Colors.xaml ile aynı değerler)
            (DurumIkon, DurumAdi, DurumRenk, DurumAciklama) = r.Durum switch
            {
                0 => ("⏳", "Beklemede", Color.FromArgb("#E8940C"),
                      r.OgretmenTarafindanOlusturuldu ? "Veli onayı bekleniyor" : "Öğretmen onayı bekleniyor"),
                1 => ("✅", "Onaylandı", Color.FromArgb("#2F9E44"), "Randevu onaylandı"),
                2 => ("❌", "Reddedildi", Color.FromArgb("#E5484D"), "Randevu reddedildi"),
                3 => ("⛔", "İptal Edildi", Color.FromArgb("#8A96A8"), "Randevu iptal edildi"),
                4 => ("✔", "Tamamlandı", Color.FromArgb("#4C6EF5"), "Randevu tamamlandı"),
                _ => (string.Empty, string.Empty, Colors.Gray, string.Empty)
            };

            AksiyonGorunur = false;
            OnaylaGorunur = false;
            ReddetGorunur = false;
            IptalGorunur = false;

            if (r.Durum != 0) return;
            if (!KullaniciOturum.VeliMi && !KullaniciOturum.OgretmenMi) return;

            bool olusturucuMu =
                (KullaniciOturum.VeliMi && !r.OgretmenTarafindanOlusturuldu) ||
                (KullaniciOturum.OgretmenMi && r.OgretmenTarafindanOlusturuldu);

            AksiyonGorunur = true;
            if (olusturucuMu)
                IptalGorunur = true;
            else
            {
                OnaylaGorunur = true;
                ReddetGorunur = true;
            }
        }
    }
}
