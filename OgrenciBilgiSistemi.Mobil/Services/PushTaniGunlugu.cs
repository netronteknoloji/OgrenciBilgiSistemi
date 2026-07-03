namespace OgrenciBilgiSistemi.Mobil.Services
{
    /// <summary>
    /// Push kayıt zincirinin cihaz üstü teşhis günlüğü. Release build'de Debug.WriteLine
    /// derlenmediği için izler Console'a VE Preferences'a yazılır; PushTaniView bu günlüğü
    /// kullanıcıya gösterir. Token değeri asla loglanmaz (yalnız uzunluk).
    /// AppDelegate gibi erken yaşam döngüsü noktalarından da çağrılabilir.
    /// </summary>
    public static class PushTaniGunlugu
    {
        public const string AnahtarTaniGunlugu = "push_tani_durumu";
        private const int MaksSatir = 25;

        private static readonly object _kilit = new();

        public static void Ekle(string mesaj)
        {
            // NSException gibi çok satırlı mesajlar (stack trace) tek satıra indirilir ve
            // kırpılır; yoksa 25 satırlık pencereyi tek mesaj yutar, asıl hata satırı kaybolur.
            mesaj = mesaj.Replace("\r", " ").Replace("\n", " ¶ ");
            if (mesaj.Length > 500)
                mesaj = mesaj[..500] + " …(kırpıldı)";

            var satir = $"{DateTime.Now:HH:mm:ss} {mesaj}";
            Console.WriteLine($"[PUSH] {satir}");

            try
            {
                lock (_kilit)
                {
                    var mevcut = Preferences.Default.Get(AnahtarTaniGunlugu, string.Empty);
                    var satirlar = string.IsNullOrEmpty(mevcut)
                        ? new List<string>()
                        : mevcut.Split('\n').ToList();

                    satirlar.Add(satir);
                    if (satirlar.Count > MaksSatir)
                        satirlar.RemoveRange(0, satirlar.Count - MaksSatir);

                    Preferences.Default.Set(AnahtarTaniGunlugu, string.Join('\n', satirlar));
                }
            }
            catch { /* Preferences erişilemezse teşhis akışı kırılmasın */ }
        }

        public static string Oku()
        {
            try
            {
                return Preferences.Default.Get(AnahtarTaniGunlugu, string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static void Temizle()
        {
            try
            {
                Preferences.Default.Remove(AnahtarTaniGunlugu);
            }
            catch { }
        }
    }
}
