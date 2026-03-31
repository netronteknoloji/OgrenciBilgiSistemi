namespace OgrenciBilgiSistemi.Helpers
{
    public static class AkademikDonemHelper
    {
        public static int Current() =>
            DateTime.Now.Month >= 9 ? DateTime.Now.Year : DateTime.Now.Year - 1;

        public static int FromDate(DateTime dt) =>
            dt.Month >= 9 ? dt.Year : dt.Year - 1;
    }
}
