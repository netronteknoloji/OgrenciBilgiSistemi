using System.ComponentModel;
using System.Runtime.CompilerServices;
using OgrenciBilgiSistemi.Mobil.Models;
using Microsoft.Maui.Graphics;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public class OgrenciViewModel : INotifyPropertyChanged
    {
        #region Gizli Değişkenler
        private Ogrenci _studentData;
        private int _selectedStatusId = 1;
        private int _serviceStatusId = 0;
        #endregion

        #region Kamu Özellikleri
        public Ogrenci OgrenciData
        {
            get => _studentData ??= new Ogrenci();
            set => SetProperty(ref _studentData, value);
        }

        public int SelectedStatusId
        {
            get => _selectedStatusId;
            set => SetProperty(ref _selectedStatusId, value);
        }

        public int ServiceStatusId
        {
            get => _serviceStatusId;
            set
            {
                if (SetProperty(ref _serviceStatusId, value))
                {
                    // Bağımlı özelliklerin UI tarafında yenilenmesini tetikler
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        // --- API'den Gelen Duruma Göre UI Hesaplamaları ---
        public string StatusIcon => ServiceStatusId switch
        {
            1 => "✓", // Bindi
            2 => "X", // Binmedi
            _ => "?"  // Bekliyor
        };

        public Color StatusColor => ServiceStatusId switch
        {
            1 => Color.FromArgb("#2ECC71"),
            2 => Color.FromArgb("#E74C3C"),
            _ => Color.FromArgb("#BDC3C7")
        };

        public string StatusText => ServiceStatusId switch
        {
            1 => "Araca Bindi",
            2 => "Araca Binmedi",
            _ => "Bekliyor..."
        };
        #endregion

        #region Arayüz Güncelleme Mekanizması
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Değer değiştiyse atama yapar ve UI'ı bilgilendirir (Kod tekrarını azaltır)
        /// </summary>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Object.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public OgrenciViewModel()
        {
            _studentData = new Ogrenci();
        }
    }
}