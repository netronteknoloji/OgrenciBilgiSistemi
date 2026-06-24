using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Shared.Constants;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Helpers;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    // Görünüm verileri — View'ın dinamik render için kullandığı sade tipler.
    public record YoklamaHucreVm(int GunIndex, int DersIndex, string RenkHex);
    public record GirisCikisKaydiVm(DateTime? Giris, DateTime? Cikis);
    public record GirisCikisGunVm(DateTime Gun, IReadOnlyList<GirisCikisKaydiVm> Kayitlar);
    public record YemekhaneGunVm(DateTime Gun, DateTime? IlkGiris);
    public record ServisYoklamaGunVm(DateTime Gun, bool SabahVar, int SabahDurum, bool AksamVar, int AksamDurum);

    public partial class OgrenciDetayGorunumModel : ObservableObject
    {
        private static readonly string[] GunAdlari = ["Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi", "Pazar"];

        private readonly int _ogrenciId;
        private readonly OgrenciService _ogrenciService;
        private readonly ServisService _servisService;
        private DateTime _currentWeekStart;
        private CancellationTokenSource? _haftaCts;

        [ObservableProperty] private string ogrenciAdSoyad = string.Empty;
        [ObservableProperty] private string ogrenciGorsel = "user_icon.png";
        [ObservableProperty] private string birimAd = string.Empty;
        [ObservableProperty] private string ogrenciNo = string.Empty;
        [ObservableProperty] private string ogrenciKartNo = string.Empty;
        [ObservableProperty] private string plaka = string.Empty;
        [ObservableProperty] private string ogretmenAdSoyad = string.Empty;
        [ObservableProperty] private string veliAdSoyad = string.Empty;
        [ObservableProperty] private string veliTelefon = string.Empty;
        [ObservableProperty] private string veliEmail = string.Empty;
        [ObservableProperty] private string veliAdres = string.Empty;
        [ObservableProperty] private string veliMeslek = string.Empty;
        [ObservableProperty] private string veliIsYeri = string.Empty;
        [ObservableProperty] private string haftaMetni = "Yükleniyor...";
        [ObservableProperty] private string yoklamaOraniMetni = "-%";
        [ObservableProperty] private string devamsizlikMetni = "-";
        [ObservableProperty] private IReadOnlyList<YoklamaHucreVm> yoklamaHucreler = [];
        [ObservableProperty] private IReadOnlyList<GirisCikisGunVm> girisCikisKayitlari = [];
        [ObservableProperty] private IReadOnlyList<YemekhaneGunVm> yemekhaneKayitlari = [];
        [ObservableProperty] private IReadOnlyList<ServisYoklamaGunVm> servisYoklamaKayitlari = [];

        public OgrenciDetayGorunumModel(int ogrenciId, OgrenciService ogrenciService, ServisService servisService)
        {
            _ogrenciId = ogrenciId;
            _ogrenciService = ogrenciService;
            _servisService = servisService;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            await OgrenciDetayYukleAsync();
            SetCurrentWeek(DateTime.Today);
        }

        [RelayCommand]
        void OncekiHafta() => SetCurrentWeek(_currentWeekStart.AddDays(-7));

        [RelayCommand]
        void SonrakiHafta() => SetCurrentWeek(_currentWeekStart.AddDays(7));

        private void SetCurrentWeek(DateTime referenceDate)
        {
            _currentWeekStart = HaftaHesaplayici.PazartesiBul(referenceDate);
            var weekEnd = _currentWeekStart.AddDays(6);
            HaftaMetni = $"{_currentWeekStart:dd.MM.yyyy} - {weekEnd:dd.MM.yyyy}";

            _haftaCts?.Cancel();
            _haftaCts = new CancellationTokenSource();
            var ct = _haftaCts.Token;

            _ = YoklamaMatrisiYukleAsync(ct);
            _ = GecisKayitlariYukleAsync(ct);
            _ = ServisYoklamaYukleAsync(ct);
        }

        private async Task OgrenciDetayYukleAsync()
        {
            try
            {
                var detay = await _ogrenciService.OgrenciDetayGetirAsync(_ogrenciId);
                if (detay == null) return;

                OgrenciAdSoyad = detay.OgrenciAdSoyad ?? string.Empty;
                OgrenciGorsel = Constants.GorselUrl(detay.OgrenciGorsel);
                BirimAd = detay.BirimAd ?? string.Empty;
                OgrenciNo = detay.OgrenciNo ?? string.Empty;
                OgrenciKartNo = detay.OgrenciKartNo ?? string.Empty;
                Plaka = detay.Plaka ?? string.Empty;
                OgretmenAdSoyad = detay.OgretmenAdSoyad ?? string.Empty;
                VeliAdSoyad = detay.VeliAdSoyad ?? string.Empty;
                VeliTelefon = detay.VeliTelefon ?? string.Empty;
                VeliEmail = detay.VeliEmail ?? string.Empty;
                VeliAdres = detay.VeliAdres ?? string.Empty;
                VeliMeslek = detay.VeliMeslek ?? string.Empty;
                VeliIsYeri = detay.VeliIsYeri ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HATA] Öğrenci detayları yüklenemedi: {ex.Message}");
                await Application.Current!.MainPage!.DisplayAlert("Hata", "Öğrenci bilgileri yüklenirken bir sorun oluştu.", "Tamam");
            }
        }

        private async Task YoklamaMatrisiYukleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var weeklyRecords = await _ogrenciService.HaftalikYoklamaGetirAsync(
                    _ogrenciId, _currentWeekStart, _currentWeekStart.AddDays(6));

                ct.ThrowIfCancellationRequested();

                var hucreler = new List<YoklamaHucreVm>();
                int totalAbsent = 0;
                int recordedLessonsCount = 0;

                for (int dayIndex = 0; dayIndex < 5; dayIndex++)
                {
                    var targetDate = _currentWeekStart.AddDays(dayIndex);
                    var dayRecord = weeklyRecords?.FirstOrDefault(r => r.OlusturulmaTarihi.Date == targetDate.Date);

                    for (int lessonIndex = 1; lessonIndex <= 8; lessonIndex++)
                    {
                        int statusId = dayRecord?.DersGetir(lessonIndex) ?? 0;
                        if (statusId is >= 1 and <= 7)
                        {
                            recordedLessonsCount++;
                            hucreler.Add(new YoklamaHucreVm(dayIndex, lessonIndex, YoklamaRenkleri.HexGetir(statusId)));
                            if (statusId == (int)YoklamaDurumu.Gelmedi) totalAbsent++;
                        }
                    }
                }

                YoklamaOraniMetni = recordedLessonsCount > 0
                    ? $"%{Math.Max(0, ((double)(recordedLessonsCount - totalAbsent) / recordedLessonsCount) * 100):0}"
                    : "%0";
                DevamsizlikMetni = $"{totalAbsent} Ders";
                YoklamaHucreler = hucreler;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HATA] Matris: {ex.Message}");
            }
        }

        private async Task GecisKayitlariYukleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var kayitlar = await _ogrenciService.HaftalikGecisKayitGetirAsync(
                    _ogrenciId, _currentWeekStart, _currentWeekStart.AddDays(6));

                ct.ThrowIfCancellationRequested();

                var anaKapiGunler = kayitlar
                    .Where(k => k.IstasyonTipi == IstasyonTipi.AnaKapi)
                    .GroupBy(k => (k.GirisTarihi ?? k.CikisTarihi)?.Date)
                    .Where(g => g.Key.HasValue)
                    .OrderBy(g => g.Key)
                    .Select(g => new GirisCikisGunVm(
                        g.Key!.Value,
                        g.OrderBy(k => k.GirisTarihi ?? k.CikisTarihi)
                         .Select(k => new GirisCikisKaydiVm(k.GirisTarihi, k.CikisTarihi))
                         .ToList()))
                    .ToList();

                var yemekhaneGunler = kayitlar
                    .Where(k => k.IstasyonTipi == IstasyonTipi.Yemekhane)
                    .GroupBy(k => k.GirisTarihi?.Date)
                    .Where(g => g.Key.HasValue)
                    .OrderBy(g => g.Key)
                    .Select(g => new YemekhaneGunVm(g.Key!.Value, g.First().GirisTarihi))
                    .ToList();

                GirisCikisKayitlari = anaKapiGunler;
                YemekhaneKayitlari = yemekhaneGunler;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HATA] Geçiş kayıtları: {ex.Message}");
            }
        }

        private async Task ServisYoklamaYukleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var kayitlar = await _servisService.OgrenciYoklamaGecmisiGetirAsync(
                    _ogrenciId, _currentWeekStart, _currentWeekStart.AddDays(6));

                ct.ThrowIfCancellationRequested();

                var enSon = new Dictionary<(DateTime Gun, int Periyot), int>();
                foreach (var k in kayitlar)
                {
                    var key = (k.Tarih.Date, k.Periyot);
                    if (!enSon.ContainsKey(key))
                        enSon[key] = k.DurumId;
                }

                var gunler = new List<ServisYoklamaGunVm>();
                for (int i = 0; i < 7; i++)
                {
                    var gun = _currentWeekStart.AddDays(i);
                    bool sabahVar = enSon.TryGetValue((gun, 1), out var sabahDurum);
                    bool aksamVar = enSon.TryGetValue((gun, 2), out var aksamDurum);
                    if (sabahVar || aksamVar)
                        gunler.Add(new ServisYoklamaGunVm(gun, sabahVar, sabahDurum, aksamVar, aksamDurum));
                }

                ServisYoklamaKayitlari = gunler;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HATA] Servis yoklama: {ex.Message}");
            }
        }

        public static string GunAdi(DateTime gun)
        {
            int gunIndex = ((int)gun.DayOfWeek + 6) % 7;
            return gunIndex < GunAdlari.Length ? GunAdlari[gunIndex] : string.Empty;
        }
    }
}
