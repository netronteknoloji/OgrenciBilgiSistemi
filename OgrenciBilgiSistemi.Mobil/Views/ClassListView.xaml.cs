#region Kullanılan Kütüphaneler
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#endregion

namespace OgrenciBilgiSistemi.Mobil.Views
{
    #region Sınıf Listesi Görünüm Mantığı
    public partial class ClassListView : ContentPage
    {
        #region Özel Değişkenler
        private readonly ClassService _classService;
        private List<ClassroomViewModel> _allClassViewModels;
        #endregion

        #region Yapıcı Metot
        public ClassListView(ClassService classService)
        {
            try
            {
                InitializeComponent();
                // DI üzerinden gelen singleton ClassService kullanılıyor
                _classService = classService;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Başlatma Hatası: {ex.Message}");
            }
        }
        #endregion

        #region Sayfa Yaşam Döngüsü Olayları
        protected override void OnAppearing()
        {
            try
            {
                base.OnAppearing();

                #region Kullanıcı Karşılama Mantığı
                // UserSession üzerinden güncel kullanıcı adını alıyoruz
                string displayName = string.IsNullOrWhiteSpace(UserSession.FullName)
                                     ? "Kullanıcı"
                                     : UserSession.FullName;

                if (WelcomeLabel != null)
                {
                    WelcomeLabel.Text = $"Merhaba {displayName} 👋";
                }
                #endregion

                // Demo modunda sarı banner'ı göster
                if (DemoBanner != null)
                    DemoBanner.IsVisible = UserSession.IsDemoMode;

                // Liste seçimi temizleniyor (Geri dönüldüğünde tekrar tıklanabilmesi için)
                if (ClassCollection != null)
                {
                    ClassCollection.SelectedItem = null;
                }

                // Verileri yükle (Task sonucunu beklemeden başlatıyoruz)
                _ = LoadClassesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnAppearing Hatası: {ex.Message}");
            }
        }
        #endregion

        #region Veri Yükleme İşlemleri
        private async Task LoadClassesAsync()
        {
            try
            {
                // UI'da veri yükleniyor görseli varsa aktif edilebilir (Opsiyonel: IsRefreshing = true)

                // API Servisimizden sınıfları çekiyoruz
                var classes = await _classService.GetAllClassesWithStudentCountAsync();

                if (classes != null)
                {
                    _allClassViewModels = classes;

                    if (ClassCollection != null)
                    {
                        // UI güncellemelerini ana iş parçacığında yapıyoruz
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ClassCollection.ItemsSource = _allClassViewModels;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Kullanıcıya hata bildirimi (API bağlantı sorunları için)
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Bağlantı Hatası", "Sınıf listesi sunucudan alınamadı. Lütfen internetinizi kontrol edin.", "Tamam");
                });
                System.Diagnostics.Debug.WriteLine($"Veri Yükleme Hatası: {ex.Message}");
            }
        }
        #endregion

        #region Arama ve Filtreleme Mantığı
        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string searchTerm = e.NewTextValue?.ToLower() ?? "";

                if (_allClassViewModels == null) return;

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    ClassCollection.ItemsSource = _allClassViewModels;
                }
                else
                {
                    var filteredList = _allClassViewModels
                        .Where(vm => vm.Name != null && vm.Name.ToLower().Contains(searchTerm))
                        .ToList();

                    ClassCollection.ItemsSource = filteredList;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Filtreleme Hatası: {ex.Message}");
            }
        }
        #endregion

        #region Navigasyon ve Etkileşim Yönetimi
        private async void OnClassFrameTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is ClassroomViewModel selectedVm)
            {
                await NavigateToStudentList(selectedVm);
            }
        }

        private async Task NavigateToStudentList(ClassroomViewModel selectedVm)
        {
            try
            {
                if (selectedVm?.ClassroomData != null)
                {
                    // StudentListView sayfasına geçiş yaparken parametreleri gönderiyoruz
                    await Navigation.PushAsync(new StudentListView(
                        selectedVm.ClassroomData.Id,
                        selectedVm.ClassroomData.Name));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", "Öğrenci listesi açılırken bir sorun oluştu.", "Tamam");
                System.Diagnostics.Debug.WriteLine($"Navigasyon Hatası: {ex.Message}");
            }
        }
        #endregion
    }
    #endregion
}