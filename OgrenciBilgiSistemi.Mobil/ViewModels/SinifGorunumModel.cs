using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    #region Sınıf Görünüm Modeli
    public class SinifGorunumModel : INotifyPropertyChanged
    {
        #region Veri Alanları
        private Models.Birim _sinifVerisi;
        private int _ogrenciSayisi;
        #endregion

        #region Kamu Özellikleri
        public Models.Birim SinifVerisi
        {
            get => _sinifVerisi ??= new Models.Birim();
            set
            {
                if (_sinifVerisi != value)
                {
                    _sinifVerisi = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Ad)); // Data değişince isim de güncellenmeli
                }
            }
        }

        public int OgrenciSayisi
        {
            get => _ogrenciSayisi;
            set
            {
                if (_ogrenciSayisi != value)
                {
                    _ogrenciSayisi = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Ad
        {
            get
            {
                try
                {
                    return !string.IsNullOrEmpty(SinifVerisi?.BirimAd)
                           ? SinifVerisi.BirimAd
                           : "Tanımsız Sınıf";
                }
                catch { return "Sınıf Bilgisi Alınamadı"; }
            }
        }
        #endregion

        #region Arayüz Güncelleme Mekanizması
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Yapıcı Metot
        public SinifGorunumModel()
        {
            _sinifVerisi = new Models.Birim();
            _ogrenciSayisi = 0;
        }
        #endregion
    }
    #endregion
}