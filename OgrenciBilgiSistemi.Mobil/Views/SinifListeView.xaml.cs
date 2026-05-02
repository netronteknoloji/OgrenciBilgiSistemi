#region Kullanılan Kütüphaneler
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
#endregion

namespace OgrenciBilgiSistemi.Mobil.Views
{
    #region Sınıf Listesi Görünüm Mantığı
    public partial class SinifListeView : ContentPage
    {
        #region Özel Değişkenler
        private readonly SinifService _sinifService;
        private List<SinifGorunumModel> _allClassViewModels;
        #endregion

        #region Yapıcı Metot
        public SinifListeView(SinifService sinifService)
        {
            try
            {
                InitializeComponent();
                // DI üzerinden gelen singleton SinifService kullanılıyor
                _sinifService = sinifService;
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
                // KullaniciOturum üzerinden güncel kullanıcı adını alıyoruz
                string displayName = string.IsNullOrWhiteSpace(KullaniciOturum.AdSoyad)
                                     ? "Kullanıcı"
                                     : KullaniciOturum.AdSoyad;

                if (WelcomeLabel != null)
                {
                    WelcomeLabel.Text = $"Merhaba {displayName} 👋";
                }
                #endregion

                // Liste seçimi temizleniyor (Geri dönüldüğünde tekrar tıklanabilmesi için)
                if (ClassCollection != null)
                {
                    ClassCollection.SelectedItem = null;
                }

                // Verileri yükle (Task sonucunu beklemeden başlatıyoruz)
                _ = LoadClassesAsync();

                // Okunmamış bildirim sayısını güncelle
                _ = BildirimBadgeGuncelle();
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
                var classes = await _sinifService.TumSiniflariOgrenciSayisiIleGetirAsync();

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
                        .Where(vm => vm.Ad != null && vm.Ad.ToLower().Contains(searchTerm))
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
            if (e.Parameter is SinifGorunumModel selectedVm)
            {
                await NavigateToStudentList(selectedVm);
            }
        }

        private async Task NavigateToStudentList(SinifGorunumModel selectedVm)
        {
            try
            {
                if (selectedVm?.SinifVerisi != null)
                {
                    // OgrenciListeView sayfasına geçiş yaparken parametreleri gönderiyoruz
                    await Navigation.PushAsync(new OgrenciListeView(
                        selectedVm.SinifVerisi.BirimId,
                        selectedVm.SinifVerisi.BirimAd));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", "Öğrenci listesi açılırken bir sorun oluştu.", "Tamam");
                System.Diagnostics.Debug.WriteLine($"Navigasyon Hatası: {ex.Message}");
            }
        }
        #endregion

        #region Randevu ve Bildirim Navigasyon
        private async Task BildirimBadgeGuncelle()
        {
            try
            {
                var bildirimService = Application.Current.MainPage.Handler.MauiContext.Services.GetService<BildirimService>();
                var sayi = await bildirimService.OkunmamisSayisiGetir();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (sayi > 0)
                    {
                        BildirimBadge.IsVisible = true;
                        BildirimSayiLabel.Text = sayi > 9 ? "9+" : sayi.ToString();
                    }
                    else
                    {
                        BildirimBadge.IsVisible = false;
                    }
                });
            }
            catch { }
        }

        private async void OnRandevularTapped(object sender, TappedEventArgs e)
        {
            var randevuService = Application.Current.MainPage.Handler.MauiContext.Services.GetService<RandevuService>();
            await Navigation.PushAsync(new RandevuListeView(randevuService));
        }

        private async void OnRandevuTakvimTapped(object sender, TappedEventArgs e)
        {
            var ogretmenRandevuService = Application.Current.MainPage.Handler.MauiContext.Services.GetService<OgretmenRandevuService>();
            await Navigation.PushAsync(new OgretmenRandevuYonetimView(ogretmenRandevuService));
        }

        private async void OnBildirimlerTapped(object sender, TappedEventArgs e)
        {
            var bildirimService = Application.Current.MainPage.Handler.MauiContext.Services.GetService<BildirimService>();
            await Navigation.PushAsync(new BildirimListeView(bildirimService));
        }

        private async void OnDuyuruYapTapped(object sender, TappedEventArgs e)
        {
            var duyuruService = Application.Current.MainPage.Handler.MauiContext.Services.GetService<DuyuruService>();
            await Navigation.PushAsync(new OgretmenDuyuruOlusturView(duyuruService));
        }
        #endregion
    }
    #endregion
}