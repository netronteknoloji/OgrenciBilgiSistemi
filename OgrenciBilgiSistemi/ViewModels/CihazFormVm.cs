using OgrenciBilgiSistemi.Models;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class CihazFormVm : IValidatableObject
    {
        public int CihazId { get; set; }

        [Required, StringLength(100)]
        public string CihazAdi { get; set; } = string.Empty;

        [Required]
        public DonanimTipi DonanimTipi { get; set; } = DonanimTipi.UsbRfid;

        [Required]
        public IstasyonTipi IstasyonTipi { get; set; } = IstasyonTipi.AnaKapi;

        public bool Aktif { get; set; } = true;

        [StringLength(45)]
        [RegularExpression(
            @"^$|^(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)$",
            ErrorMessage = "Geçerli bir IPv4 adresi giriniz."
        )]
        public string? IpAdresi { get; set; }

        [Range(1, 65535, ErrorMessage = "Port 1-65535 aralığında olmalıdır.")]
        public int? PortNo { get; set; }

        public string FormAction { get; set; } = "Ekle";
        public string SubmitText { get; set; } = "Kaydet";
        public bool IncludeId { get; set; }
        public bool ShowGuid { get; set; }
        public string GuidStr { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            if (DonanimTipi == DonanimTipi.ZKTeco)
            {
                if (string.IsNullOrWhiteSpace(IpAdresi))
                    yield return new ValidationResult("ZKTeco için IP adresi zorunludur.", new[] { nameof(IpAdresi) });

                if (!PortNo.HasValue || PortNo.Value <= 0)
                    yield return new ValidationResult("ZKTeco için port numarası zorunludur.", new[] { nameof(PortNo) });
            }
        }

        public static CihazFormVm FromModel(CihazModel m) => new()
        {
            CihazId = m.CihazId,
            CihazAdi = m.CihazAdi,
            DonanimTipi = m.DonanimTipi,
            IstasyonTipi = m.IstasyonTipi,
            Aktif = m.Aktif,
            IpAdresi = m.IpAdresi,
            PortNo = m.PortNo,
            FormAction = "Guncelle",
            SubmitText = "Güncelle",
            IncludeId = true,
            ShowGuid = true,
            GuidStr = m.CihazKodu.ToString(),
        };

        public CihazModel ToModel() => new()
        {
            CihazId = CihazId,
            CihazAdi = CihazAdi,
            DonanimTipi = DonanimTipi,
            IstasyonTipi = IstasyonTipi,
            Aktif = Aktif,
            IpAdresi = IpAdresi,
            PortNo = PortNo,
        };
    }
}
