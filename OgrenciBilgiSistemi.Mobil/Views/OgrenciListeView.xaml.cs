#region Kütüphane Tanımlamaları
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.ViewModels;
#endregion

namespace OgrenciBilgiSistemi.Mobil.Views;

public partial class OgrenciListeView : ContentPage
{
    #region Özel Değişkenler ve Servisler
    private readonly OgrenciService _ogrenciService;
    private int _classId;
    private List<OgrenciGorunumModel> _studentViewModels;
    #endregion

    #region Yapıcı Metot (Constructor)
    public OgrenciListeView(int classId, string className)
    {
        try
        {
            InitializeComponent();
            _ogrenciService = IPlatformApplication.Current.Services.GetRequiredService<OgrenciService>();
            _classId = classId;
            LblClassName.Text = className;

            // Veri yüklemeyi tetikle
            LoadStudents();
        }
        catch { /**/ }
    }
    #endregion

    #region Veri Yükleme İşlemleri
    private async void LoadStudents()
    {
        try
        {
            var students = await _ogrenciService.SinifaGoreOgrencileriGetirAsync(_classId);
            if (students == null) return;

            _studentViewModels = students.Select(s => new OgrenciGorunumModel
            {
                OgrenciData = s,
                SecilenDurumId = 1 // Varsayılan: Geldi
            }).ToList();

            StudentCollection.ItemsSource = _studentViewModels;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HATA] Öğrenci listesi yüklenemedi: {ex.Message}");
            await DisplayAlert("Hata", $"Öğrenci listesi yüklenemedi:\n{ex.Message}", "Tamam");
        }
    }
    #endregion

    #region Kullanıcı Etkileşimleri
    private async void OnPeriodChanged(object sender, EventArgs e)
    {
        try
        {
            if (PeriodPicker.SelectedIndex == -1) return;
            int lessonNumber = PeriodPicker.SelectedIndex + 1;

            // O ders saatine ait kayıtlı yoklama var mı?
            var existingAttendance = await _ogrenciService.MevcutYoklamaGetirAsync(_classId, lessonNumber);
            bool hasData = existingAttendance != null && existingAttendance.Count > 0;

            // Arayüz kontrollerini güncelle
            StatusWarningFrame.IsVisible = hasData;
            BtnSave.IsVisible = !hasData;
            BtnUpdate.IsVisible = hasData;

            if (_studentViewModels != null)
            {
                foreach (var vm in _studentViewModels)
                {
                    // Eğer veritabanında bu öğrenci için o ders saatinde kayıt varsa onu getir, yoksa 'Geldi' yap
                    if (hasData && existingAttendance.TryGetValue(vm.OgrenciData.OgrenciId, out int statusId))
                        vm.SecilenDurumId = statusId;
                    else
                        vm.SecilenDurumId = 1;
                }
            }
        }
        catch { /**/ }
    }

    private async void OnSaveAttendanceClicked(object sender, EventArgs e)
    {
        await ProcessAttendance(isUpdate: false);
    }

    private async void OnUpdateAttendanceClicked(object sender, EventArgs e)
    {
        // Güncelleme butonu tıklandığında onay alarak işlemi başlatır
        bool confirm = await DisplayAlert("Onay", "Mevcut yoklama kaydını değiştirmek istediğinize emin misiniz?", "Evet", "Hayır");
        if (confirm)
        {
            await ProcessAttendance(isUpdate: true);
        }
    }

    /// <summary>
    /// Hem Kaydet hem de Güncelleme işlemini yöneten merkezi metot
    /// </summary>
    private async Task ProcessAttendance(bool isUpdate)
    {
        try
        {
            if (PeriodPicker.SelectedIndex == -1)
            {
                await DisplayAlert("Uyarı", "Lütfen önce ders saatini seçiniz!", "Tamam");
                return;
            }

            int lessonNumber = PeriodPicker.SelectedIndex + 1;
            var attendanceData = _studentViewModels
                .Select(vm => (vm.OgrenciData.OgrenciId, vm.SecilenDurumId))
                .ToList();

            // Kayıt veya Güncelleme işlemini gerçekleştir
            // Giriş yapan öğretmenin ID'si KullaniciOturum üzerinden alınır
            await _ogrenciService.TopluYoklamaKaydetAsync(attendanceData, _classId, KullaniciOturum.KullaniciId, lessonNumber);

            string message = isUpdate ? "Yoklama güncellendi." : "Yoklama başarıyla kaydedildi.";
            await DisplayAlert("Bilgi", message, "Tamam");

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"İşlem sırasında sorun çıktı: {ex.Message}", "Tamam");
        }
    }

    private void OnStatusTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (sender is Border border && border.BindingContext is OgrenciGorunumModel vm && e.Parameter != null)
            {
                if (int.TryParse(e.Parameter.ToString(), out int statusId))
                {
                    vm.SecilenDurumId = statusId;
                }
            }
        }
        catch { /**/ }
    }

    private async void OnStudentDetailClicked(object sender, EventArgs e)
    {
        try
        {
            var visualElement = sender as VisualElement;
            var selectedStudent = visualElement?.BindingContext as OgrenciGorunumModel;

            if (selectedStudent?.OgrenciData != null)
            {
                await Navigation.PushAsync(new OgrenciDetayView(selectedStudent.OgrenciData.OgrenciId));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Öğrenci detayları yüklenemedi: " + ex.Message, "Tamam");
        }
    }
    #endregion
}
