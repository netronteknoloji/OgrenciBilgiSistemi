namespace OgrenciBilgiSistemi.Abstractions
{
    /// <summary>
    /// Görsel yükleme/silme için tek giriş noktası (öğrenci, kitap, kullanıcı, öğretmen görselleri).
    /// Yalnızca JPG/PNG (içerik imzasıyla doğrulanır, en fazla 2 MB) kabul edilir; dosyalar yalnızca
    /// wwwroot/uploads altına yazılır ve yalnızca oradan silinir.
    /// </summary>
    public interface IFileStorage
    {
        /// <summary>
        /// Görseli kaydeder ve "/uploads/[altKlasor/]ad.uzanti" döndürür. Kayıt başarılıysa
        /// <paramref name="existingPath"/> (uploads altındaysa) silinir. Dosya yoksa existingPath döner.
        /// Geçersiz dosyada InvalidOperationException fırlatır.
        /// </summary>
        Task<string?> SaveImageAsync(IFormFile file, string? existingPath = null, CancellationToken ct = default, string? altKlasor = null);

        /// <summary>Göreli yol uploads altındaysa dosyayı siler; değilse hiçbir şey yapmaz.</summary>
        void Sil(string? goreliYol);
    }
}
