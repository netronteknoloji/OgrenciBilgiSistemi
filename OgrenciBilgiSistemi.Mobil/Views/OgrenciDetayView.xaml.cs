using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.ViewModels;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Shared.Constants;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class OgrenciDetayView : ContentPage
    {
        private readonly int _studentId;
        private readonly OgrenciService _ogrenciService;
        private readonly ServisService _servisService;
        private DateTime currentWeekStart;
        private CancellationTokenSource _haftaCts;

        // Geçerli yoklama durum aralığı (1-7); Shared/YoklamaDurumu enum ile aynı.
        private static bool GecerliDurum(int statusId) => statusId is >= 1 and <= 7;

        public OgrenciDetayView(int studentId, OgrenciService? ogrenciService = null, ServisService? servisService = null)
        {
            InitializeComponent();
            _studentId = studentId;
            _ogrenciService = ogrenciService ?? IPlatformApplication.Current.Services.GetRequiredService<OgrenciService>();
            _servisService = servisService ?? IPlatformApplication.Current.Services.GetRequiredService<ServisService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SetCurrentWeek(DateTime.Today);
            await LoadStudentDetails();
        }

        private static readonly string[] GunAdlari = { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi", "Pazar" };

        private void SetCurrentWeek(DateTime referenceDate)
        {
            currentWeekStart = HaftaHesaplayici.PazartesiBul(referenceDate);
            DateTime currentWeekEnd = currentWeekStart.AddDays(6);
            LblWeekRange.Text = $"{currentWeekStart:dd.MM.yyyy} - {currentWeekEnd:dd.MM.yyyy}";

            _ = LoadWeeklyAttendanceMatrix();
            _ = LoadHaftalikGecisKayitlari();
            _ = LoadHaftalikServisYoklama();
        }

        private void OnPreviousWeekClicked(object sender, EventArgs e) => SetCurrentWeek(currentWeekStart.AddDays(-7));
        private void OnNextWeekClicked(object sender, EventArgs e) => SetCurrentWeek(currentWeekStart.AddDays(7));

        private async Task LoadStudentDetails()
        {
            try
            {
                var detay = await _ogrenciService.OgrenciDetayGetirAsync(_studentId);
                if (detay != null)
                {
                    var vm = new OgrenciGorunumModel();
                    vm.OgrenciData.OgrenciId = _studentId;
                    vm.OgrenciData.OgrenciAdSoyad = detay.OgrenciAdSoyad;
                    vm.OgrenciData.OgrenciGorsel = Constants.GorselUrl(detay.OgrenciGorsel);
                    this.BindingContext = vm;

                    LblClass.Text         = detay.BirimAd;
                    LblStudentNo.Text     = detay.OgrenciNo;
                    LblCardNo.Text        = detay.OgrenciKartNo;
                    LblServicePlate.Text  = detay.Plaka;
                    LblParentName.Text    = detay.VeliAdSoyad;
                    LblParentPhone.Text   = detay.VeliTelefon;
                    LblParentEmail.Text   = detay.VeliEmail;
                    LblParentAddress.Text = detay.VeliAdres;
                    LblParentJob.Text     = detay.VeliMeslek;
                    LblParentWork.Text    = detay.VeliIsYeri;
                    LblTeacherName.Text   = detay.OgretmenAdSoyad;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HATA] Öğrenci detayları yüklenemedi: {ex.Message}");
                await DisplayAlert("Hata", "Öğrenci bilgileri yüklenirken bir sorun oluştu.", "Tamam");
            }
        }

        private async Task LoadWeeklyAttendanceMatrix()
        {
            // Önceki hafta yükleme isteğini iptal et
            _haftaCts?.Cancel();
            _haftaCts = new CancellationTokenSource();
            var ct = _haftaCts.Token;

            try
            {
                ct.ThrowIfCancellationRequested();
                var weeklyRecords = await _ogrenciService.HaftalikYoklamaGetirAsync(_studentId, currentWeekStart, currentWeekStart.AddDays(6));

                // Dinamik kutucukları temizle
                var cellsToRemove = GridAttendanceMatrix.Children
                    .Cast<View>()
                    .Where(c => Grid.GetRow(c) > 0 && Grid.GetColumn(c) >= 1 && Grid.GetColumn(c) <= 5)
                    .ToList();

                foreach (var cell in cellsToRemove)
                    GridAttendanceMatrix.Children.Remove(cell);

                int totalAbsent = 0;
                int recordedLessonsCount = 0;

                for (int dayIndex = 0; dayIndex < 5; dayIndex++)
                {
                    DateTime targetDate = currentWeekStart.AddDays(dayIndex);
                    var dayRecord = weeklyRecords?.FirstOrDefault(r => r.OlusturulmaTarihi.Date == targetDate.Date);

                    for (int lessonIndex = 1; lessonIndex <= 8; lessonIndex++)
                    {
                        int statusId = 0;
                        if (dayRecord != null)
                        {
                            statusId = dayRecord.DersGetir(lessonIndex) ?? 0;
                        }

                        if (GecerliDurum(statusId))
                        {
                            recordedLessonsCount++;

                            var box = new Border
                            {
                                BackgroundColor = Color.FromArgb(YoklamaRenkleri.HexGetir(statusId)),
                                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                                Margin = new Thickness(1),
                                HeightRequest = 25,
                                WidthRequest = 25
                            };

                            GridAttendanceMatrix.Add(box, dayIndex + 1, lessonIndex);
                            if (statusId == (int)YoklamaDurumu.Gelmedi) totalAbsent++;
                        }
                    }
                }

                UpdateAttendanceUI(recordedLessonsCount, totalAbsent);
            }
            catch (OperationCanceledException) { /* Yeni hafta seçildi, eski istek iptal edildi */ }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HATA] Matris: {ex.Message}");
            }
        }

        private void UpdateAttendanceUI(int total, int absent)
        {
            if (total > 0)
            {
                double rate = ((double)(total - absent) / total) * 100;
                LblAttendanceRate.Text = $"%{Math.Max(0, rate):0}";
            }
            else
            {
                LblAttendanceRate.Text = "%0";
            }
            LblAbsenteeismCount.Text = $"{absent} Ders";
        }

        private async Task LoadHaftalikGecisKayitlari()
        {
            _haftaCts?.Token.ThrowIfCancellationRequested();
            var ct = _haftaCts?.Token ?? CancellationToken.None;

            try
            {
                ct.ThrowIfCancellationRequested();
                var kayitlar = await _ogrenciService.HaftalikGecisKayitGetirAsync(_studentId, currentWeekStart, currentWeekStart.AddDays(6));

                var anaKapiKayitlari = kayitlar
                    .Where(k => k.IstasyonTipi == IstasyonTipi.AnaKapi)
                    .GroupBy(k => (k.GirisTarihi ?? k.CikisTarihi)?.Date)
                    .Where(g => g.Key.HasValue)
                    .OrderBy(g => g.Key)
                    .ToList();

                var yemekhaneKayitlari = kayitlar
                    .Where(k => k.IstasyonTipi == IstasyonTipi.Yemekhane)
                    .GroupBy(k => k.GirisTarihi?.Date)
                    .Where(g => g.Key.HasValue)
                    .OrderBy(g => g.Key)
                    .ToList();

                ct.ThrowIfCancellationRequested();

                LayoutGirisCikis.Children.Clear();
                if (anaKapiKayitlari.Count == 0)
                {
                    LayoutGirisCikis.Children.Add(new Label
                    {
                        Text = "Bu hafta giriş/çıkış kaydı bulunmuyor.",
                        FontSize = 12, TextColor = Color.FromArgb("#BDC3C7"), HorizontalOptions = LayoutOptions.Center
                    });
                }
                else
                {
                    foreach (var gun in anaKapiKayitlari)
                    {
                        int gunIndex = ((int)gun.Key.Value.DayOfWeek + 6) % 7;
                        string gunAdi = gunIndex < GunAdlari.Length ? GunAdlari[gunIndex] : "";

                        LayoutGirisCikis.Children.Add(new Label
                        {
                            Text = $"{gunAdi}, {gun.Key.Value:dd.MM.yyyy}",
                            FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C3E50")
                        });

                        foreach (var kayit in gun.OrderBy(k => k.GirisTarihi ?? k.CikisTarihi))
                        {
                            var satir = new HorizontalStackLayout { Spacing = 10, Margin = new Thickness(10, 2, 0, 0) };

                            if (kayit.GirisTarihi.HasValue)
                            {
                                satir.Children.Add(new Label
                                {
                                    Text = $"Giriş: {kayit.GirisTarihi.Value:HH:mm}",
                                    FontSize = 13, TextColor = Color.FromArgb(YoklamaRenkleri.GirisHex), FontAttributes = FontAttributes.Bold
                                });
                            }
                            if (kayit.CikisTarihi.HasValue)
                            {
                                satir.Children.Add(new Label
                                {
                                    Text = $"Çıkış: {kayit.CikisTarihi.Value:HH:mm}",
                                    FontSize = 13, TextColor = Color.FromArgb(YoklamaRenkleri.CikisHex), FontAttributes = FontAttributes.Bold
                                });
                            }

                            LayoutGirisCikis.Children.Add(satir);
                        }
                    }
                }

                LayoutYemekhane.Children.Clear();
                if (yemekhaneKayitlari.Count == 0)
                {
                    LayoutYemekhane.Children.Add(new Label
                    {
                        Text = "Bu hafta yemekhane kaydı bulunmuyor.",
                        FontSize = 12, TextColor = Color.FromArgb("#BDC3C7"), HorizontalOptions = LayoutOptions.Center
                    });
                }
                else
                {
                    foreach (var gun in yemekhaneKayitlari)
                    {
                        int gunIndex = ((int)gun.Key.Value.DayOfWeek + 6) % 7;
                        string gunAdi = gunIndex < GunAdlari.Length ? GunAdlari[gunIndex] : "";

                        var satir = new HorizontalStackLayout { Spacing = 10 };
                        satir.Children.Add(new Label
                        {
                            Text = $"{gunAdi}, {gun.Key.Value:dd.MM.yyyy}",
                            FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C3E50")
                        });

                        var ilkKayit = gun.First();
                        if (ilkKayit.GirisTarihi.HasValue)
                        {
                            satir.Children.Add(new Label
                            {
                                Text = $"— {ilkKayit.GirisTarihi.Value:HH:mm}",
                                FontSize = 13, TextColor = Color.FromArgb("#E67E22"), FontAttributes = FontAttributes.Bold
                            });
                        }

                        LayoutYemekhane.Children.Add(satir);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HATA] Geçiş kayıtları: {ex.Message}");
            }
        }

        private async Task LoadHaftalikServisYoklama()
        {
            _haftaCts?.Token.ThrowIfCancellationRequested();
            var ct = _haftaCts?.Token ?? CancellationToken.None;

            try
            {
                ct.ThrowIfCancellationRequested();
                var kayitlar = await _servisService.OgrenciYoklamaGecmisiGetirAsync(_studentId, currentWeekStart, currentWeekStart.AddDays(6));
                ct.ThrowIfCancellationRequested();

                // Aynı (gün, periyot) için en son kaydı tut (sorgu zaten Tarih DESC döndürüyor)
                var enSon = new Dictionary<(DateTime Gun, int Periyot), int>();
                foreach (var k in kayitlar)
                {
                    var key = (k.Tarih.Date, k.Periyot);
                    if (!enSon.ContainsKey(key))
                        enSon[key] = k.DurumId;
                }

                LayoutServisYoklama.Children.Clear();
                if (enSon.Count == 0)
                {
                    LayoutServisYoklama.Children.Add(new Label
                    {
                        Text = "Bu hafta servis yoklama kaydı bulunmuyor.",
                        FontSize = 12, TextColor = Color.FromArgb("#BDC3C7"), HorizontalOptions = LayoutOptions.Center
                    });
                    return;
                }

                for (int i = 0; i < 7; i++)
                {
                    DateTime gun = currentWeekStart.AddDays(i);
                    bool sabahVar = enSon.TryGetValue((gun, 1), out var sabahDurum);
                    bool aksamVar = enSon.TryGetValue((gun, 2), out var aksamDurum);
                    if (!sabahVar && !aksamVar) continue;

                    int gunIndex = ((int)gun.DayOfWeek + 6) % 7;
                    string gunAdi = gunIndex < GunAdlari.Length ? GunAdlari[gunIndex] : "";

                    LayoutServisYoklama.Children.Add(new Label
                    {
                        Text = $"{gunAdi}, {gun:dd.MM.yyyy}",
                        FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C3E50")
                    });

                    var satir = new HorizontalStackLayout { Spacing = 12, Margin = new Thickness(10, 2, 0, 0) };
                    satir.Children.Add(YoklamaEtiketi("Sabah", sabahVar, sabahDurum));
                    satir.Children.Add(YoklamaEtiketi("Akşam", aksamVar, aksamDurum));
                    LayoutServisYoklama.Children.Add(satir);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HATA] Servis yoklama: {ex.Message}");
            }
        }

        private static Label YoklamaEtiketi(string periyotAdi, bool kayitVar, int durumId)
        {
            string isaret;
            string renk;
            if (!kayitVar)
            {
                isaret = "—";
                renk = "#BDC3C7";
            }
            else if (durumId == 1) // Bindi
            {
                isaret = "✓";
                renk = YoklamaRenkleri.GirisHex;
            }
            else // Binmedi (2) veya bilinmeyen
            {
                isaret = "✗";
                renk = YoklamaRenkleri.CikisHex;
            }

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
                string phoneNumber = LblParentPhone.Text?.Trim();
                if (PhoneDialer.Default.IsSupported && !string.IsNullOrEmpty(phoneNumber) && phoneNumber != "-")
                    PhoneDialer.Default.Open(phoneNumber);
            }
            catch { /**/ }
        }
    }
}