using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    #region Sınıf Görünüm Modeli
    public class ClassroomViewModel : INotifyPropertyChanged
    {
        #region Veri Alanları
        private Models.Birim _classroomData;
        private int _studentCount;
        #endregion

        #region Kamu Özellikleri
        public Models.Birim ClassroomData
        {
            get => _classroomData ??= new Models.Birim();
            set
            {
                if (_classroomData != value)
                {
                    _classroomData = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Name)); // Data değişince isim de güncellenmeli
                }
            }
        }

        public int StudentCount
        {
            get => _studentCount;
            set
            {
                if (_studentCount != value)
                {
                    _studentCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Name
        {
            get
            {
                try
                {
                    return !string.IsNullOrEmpty(ClassroomData?.Name)
                           ? ClassroomData.Name
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
        public ClassroomViewModel()
        {
            _classroomData = new Models.Birim();
            _studentCount = 0;
        }
        #endregion
    }
    #endregion
}