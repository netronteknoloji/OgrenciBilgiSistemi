using System.ComponentModel;
using System.Runtime.CompilerServices;
using OgrenciBilgiSistemi.Mobil.Models;
using Microsoft.Maui.Graphics;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public class OgrenciGorunumModel : INotifyPropertyChanged
    {
        #region Gizli Değişkenler
        private Ogrenci _ogrenciVerisi;
        private int _secilenDurumId = 1;
        private int _servisDurumId = 0;
        #endregion

        #region Kamu Özellikleri
        public Ogrenci OgrenciData
        {
            get => _ogrenciVerisi ??= new Ogrenci();
            set => SetProperty(ref _ogrenciVerisi, value);
        }

        public int SecilenDurumId
        {
            get => _secilenDurumId;
            set => SetProperty(ref _secilenDurumId, value);
        }

        public int ServisDurumId
        {
            get => _servisDurumId;
            set
            {
                if (SetProperty(ref _servisDurumId, value))
                {
                    // Bağımlı özelliklerin UI tarafında yenilenmesini tetikler
                    OnPropertyChanged(nameof(DurumIkon));
                    OnPropertyChanged(nameof(DurumRenk));
                    OnPropertyChanged(nameof(DurumMetin));
                }
            }
        }

        // --- API'den Gelen Duruma Göre UI Hesaplamaları ---
        public string DurumIkon => ServisDurumId switch
        {
            1 => "✓", // Bindi
            2 => "X", // Binmedi
            _ => "?"  // Bekliyor
        };

        public Color DurumRenk => ServisDurumId switch
        {
            1 => Color.FromArgb("#2ECC71"),
            2 => Color.FromArgb("#E74C3C"),
            _ => Color.FromArgb("#BDC3C7")
        };

        public string DurumMetin => ServisDurumId switch
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

        public OgrenciGorunumModel()
        {
            _ogrenciVerisi = new Ogrenci();
        }
    }
}