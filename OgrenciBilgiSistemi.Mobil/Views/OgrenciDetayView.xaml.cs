using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.ViewModels;
using OgrenciBilgiSistemi.Mobil.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
// Border ve Shape bileşenleri için gerekli:
using Microsoft.Maui.Controls.Shapes;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class OgrenciDetayView : ContentPage
    {
        private readonly int _studentId;
        private readonly OgrenciService _ogrenciService;
        private DateTime currentWeekStart;

        private readonly Dictionary<int, Color> StatusColors = new()
        {
            { 1, Color.FromArgb("#1ABC9C") }, // Geldi
            { 2, Color.FromArgb("#E74C3C") }, // Gelmedi
            { 3, Color.FromArgb("#F1C40F") }, // Geç Geldi
            { 4, Color.FromArgb("#3498DB") }, // İzinli
            { 5, Color.FromArgb("#9B59B6") }, // Raporlu
            { 6, Color.FromArgb("#34495E") }, // Nöbetçi
            { 7, Color.FromArgb("#95A5A6") }  // Görevli
        };

        public OgrenciDetayView(int studentId)
        {
            InitializeComponent();
            _studentId = studentId;
            _ogrenciService = IPlatformApplication.Current.Services.GetRequiredService<OgrenciService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SetCurrentWeek(DateTime.Today);
            await LoadStudentDetails();
        }

        private void SetCurrentWeek(DateTime referenceDate)
        {
            int diff = (7 + (referenceDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            currentWeekStart = referenceDate.AddDays(-diff).Date;

            DateTime currentWeekEnd = currentWeekStart.AddDays(6);
            LblWeekRange.Text = $"{currentWeekStart:dd.MM.yyyy} - {currentWeekEnd:dd.MM.yyyy}";

            _ = LoadWeeklyAttendanceMatrix();
        }

        private void OnPreviousWeekClicked(object sender, EventArgs e) => SetCurrentWeek(currentWeekStart.AddDays(-7));
        private void OnNextWeekClicked(object sender, EventArgs e) => SetCurrentWeek(currentWeekStart.AddDays(7));

        private async Task LoadStudentDetails()
        {
            try
            {
                var details = await _ogrenciService.OgrenciDetayGetirAsync(_studentId);
                if (details != null)
                {
                    var vm = new OgrenciGorunumModel();
                    vm.OgrenciData.OgrenciId = _studentId;
                    // API Türkçe anahtar isimleri kullanıyor (OgrenciAdSoyad, VeliAdSoyad vb.)
                    vm.OgrenciData.OgrenciAdSoyad = details.ContainsKey("OgrenciAdSoyad") ? details["OgrenciAdSoyad"] : "Bilinmiyor";

                    string imgName = details.ContainsKey("OgrenciGorsel") ? details["OgrenciGorsel"] : "user_icon.png";
                    vm.OgrenciData.OgrenciGorsel = Constants.GorselUrl(imgName);
                    this.BindingContext = vm;

                    LblClass.Text         = details.ContainsKey("BirimAd")         ? details["BirimAd"]         : "-";
                    LblStudentNo.Text     = details.ContainsKey("OgrenciNo")        ? details["OgrenciNo"]        : "-";
                    LblCardNo.Text        = details.ContainsKey("OgrenciKartNo")    ? details["OgrenciKartNo"]    : "-";
                    LblServicePlate.Text  = details.ContainsKey("Plaka")            ? details["Plaka"]            : "Kullanmıyor";
                    LblParentName.Text    = details.ContainsKey("VeliAdSoyad")      ? details["VeliAdSoyad"]      : "-";
                    LblParentPhone.Text   = details.ContainsKey("VeliTelefon")      ? details["VeliTelefon"]      : "-";
                    LblParentEmail.Text   = details.ContainsKey("VeliEmail")        ? details["VeliEmail"]        : "-";
                    LblParentAddress.Text = details.ContainsKey("VeliAdres")        ? details["VeliAdres"]        : "-";
                    LblParentJob.Text     = details.ContainsKey("VeliMeslek")       ? details["VeliMeslek"]       : "-";
                    LblParentWork.Text    = details.ContainsKey("VeliIsYeri")       ? details["VeliIsYeri"]       : "-";
                    LblTeacherName.Text   = details.ContainsKey("OgretmenAdSoyad")  ? details["OgretmenAdSoyad"]  : "Atanmamış";

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
            try
            {
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

                        if (statusId > 0 && StatusColors.ContainsKey(statusId))
                        {
                            recordedLessonsCount++;

                            // Hata veren ToolTipProperties kaldırıldı, Border yapısı sadeleştirildi
                            var box = new Border
                            {
                                BackgroundColor = StatusColors[statusId],
                                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                                Margin = new Thickness(1),
                                HeightRequest = 25,
                                WidthRequest = 25
                            };

                            GridAttendanceMatrix.Add(box, dayIndex + 1, lessonIndex);
                            if (statusId == 2) totalAbsent++;
                        }
                    }
                }

                UpdateAttendanceUI(recordedLessonsCount, totalAbsent);
            }
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