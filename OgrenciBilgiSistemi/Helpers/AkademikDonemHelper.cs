namespace OgrenciBilgiSistemi.Helpers
{
    public static class AkademikDonemHelper
    {
        public static int Current() =>
            DateTime.Now.Month >= 9 ? DateTime.Now.Year : DateTime.Now.Year - 1;

    }
}
