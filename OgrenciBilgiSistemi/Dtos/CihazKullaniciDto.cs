namespace OgrenciBilgiSistemi.Dtos
{
    public class CihazKullaniciDto
    {
        public string EnrollNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public string Password { get; set; } = "";
        public int Privilege { get; set; } // 0: normal user
        public bool Enabled { get; set; }
        public string? CardNumber { get; set; } // bazı firmware'larda null kalabilir
    }
}

