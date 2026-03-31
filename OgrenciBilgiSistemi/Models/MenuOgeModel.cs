using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class MenuOgeModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Baslik { get; set; } = string.Empty;
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public string? GerekliRole { get; set; }
        public int Sirala { get; set; }
        public int? AnaMenuId { get; set; }
        public MenuOgeModel? AnaMenu { get; set; }
        public ICollection<MenuOgeModel> AltMenuler { get; set; } = new List<MenuOgeModel>(); 
        public ICollection<KullaniciMenuModel> KullaniciMenuler { get; set; } = new List<KullaniciMenuModel>();
    }
}
