namespace OgrenciBilgiSistemi.Shared.Helpers
{
    /// <summary>
    /// Kart numarasının tek biçimi: boşluk ve baştaki sıfırlar atılır ("000" → "0").
    /// Kaydederken ve okuturken aynı kural kullanılmalıdır; aksi halde kart eşleşmez.
    /// </summary>
    public static class KartNo
    {
        public static string? Normalize(string? kartNo)
        {
            if (string.IsNullOrWhiteSpace(kartNo))
                return null;

            var sifirsiz = kartNo.Trim().TrimStart('0');
            return sifirsiz.Length == 0 ? "0" : sifirsiz;
        }
    }
}
