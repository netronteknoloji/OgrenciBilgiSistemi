using System.Globalization;

namespace OgrenciBilgiSistemi.Shared.Kurallar
{
    public sealed record YemekHakkiEslesmeSonucu(
        IReadOnlyList<int> AcilacakIdler,
        IReadOnlyList<int> Bulunamayan,
        IReadOnlyList<int> BirdenFazla,
        IReadOnlyList<int> GecersizSatirlar);

    /// <summary>
    /// Excel'den yemekhane ay listesi yükleme: A sütunundaki öğrenci no'larını öğrencilere eşler.
    /// Dosya o ayın tam listesidir; eşleşmeyen veya birden çok öğrenciye eşleşen no açılmaz, raporlanır.
    /// </summary>
    public static class YemekHakkiIceAktarmaKurallari
    {
        /// <param name="hucreler">A sütunu, 1. satırdan itibaren (satır no = indeks + 1).</param>
        /// <param name="noyaGoreOgrenciIdler">Aktif öğrenciler: öğrenci no → öğrenci id'leri (no unique değil).</param>
        public static YemekHakkiEslesmeSonucu Esle(
            IEnumerable<string?> hucreler,
            IReadOnlyDictionary<int, IReadOnlyList<int>> noyaGoreOgrenciIdler)
        {
            var acilacak = new List<int>();
            var bulunamayan = new List<int>();
            var birdenFazla = new List<int>();
            var gecersiz = new List<int>();
            var gorulen = new HashSet<int>();

            var satir = 0;
            var ilkDoluSatir = true;
            foreach (var hucre in hucreler)
            {
                satir++;
                var deger = hucre?.Trim();
                if (string.IsNullOrEmpty(deger))
                    continue;

                if (!NoCoz(deger, out var no))
                {
                    // İlk dolu satır sayı değilse başlıktır ("Öğrenci No")
                    if (!ilkDoluSatir)
                        gecersiz.Add(satir);
                    ilkDoluSatir = false;
                    continue;
                }
                ilkDoluSatir = false;

                if (!gorulen.Add(no))
                    continue;

                if (!noyaGoreOgrenciIdler.TryGetValue(no, out var idler) || idler.Count == 0)
                    bulunamayan.Add(no);
                else if (idler.Count > 1)
                    birdenFazla.Add(no);
                else
                    acilacak.Add(idler[0]);
            }

            return new YemekHakkiEslesmeSonucu(acilacak, bulunamayan, birdenFazla, gecersiz);
        }

        // Excel sayı hücresi metne "415" veya "415.0" olarak gelebilir; kesirli değer geçersizdir
        private static bool NoCoz(string deger, out int no)
        {
            no = 0;
            if (int.TryParse(deger, NumberStyles.Integer, CultureInfo.InvariantCulture, out no))
                return no > 0;

            if (decimal.TryParse(deger, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
                && d == decimal.Truncate(d) && d > 0 && d <= int.MaxValue)
            {
                no = (int)d;
                return true;
            }
            return false;
        }
    }
}
