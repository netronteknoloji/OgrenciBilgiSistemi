using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZKTecoWindowsService.Models
{
    public class MenuOgeModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Menüde görüntülenecek başlık (örn: "Ana Sayfa")
        [Required]
        public string Baslik { get; set; } = string.Empty;

        // Hedef Controller adı
        [Required]
        public string Controller { get; set; } = string.Empty;

        // Hedef Action adı
        [Required]
        public string Action { get; set; } = string.Empty;

        // Bu menü öğesine erişmek için gerekli rol (boş ise herkes erişebilir)
        public string? GerekliRole { get; set; }

        // Menü sıralaması
        public int Sirala { get; set; }

        // İsteğe bağlı: Alt menüler için ebeveyn kimliği (null ise ana menüdür)
        public int? AnaMenuId { get; set; }

        // Alt menü öğelerini tutmak için
        [NotMapped]
        public List<MenuOgeModel> Children { get; set; } = new();
    }
}
