namespace OgrenciBilgiSistemi.Dtos
{
    public class ZkUserDto
    {
        public string UserId { get; set; } = "";
        public string? Name { get; set; }
        public int Privilege { get; set; }
        public bool Enabled { get; set; }
        public string? CardNumber { get; set; }
    }
}
