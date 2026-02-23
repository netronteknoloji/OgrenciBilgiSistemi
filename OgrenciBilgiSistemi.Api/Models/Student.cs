namespace OgrenciBilgiSistemi.Api.Models
{
    public class Student
    {
        public int     Id              { get; set; }
        public string  FullName        { get; set; } = string.Empty;
        public int     StudentNumber   { get; set; }
        public string? ImagePath       { get; set; }
        public string? CardNumber      { get; set; }
        public string? ClassName       { get; set; }
        public string? ParentFullName  { get; set; }
        public string? ParentPhoneNumber { get; set; }
        public int     ExitStatus      { get; set; }
        public bool    IsActive        { get; set; }
        public int?    TeacherId       { get; set; }
        public int?    UnitId          { get; set; }
        public int?    PersonnelId     { get; set; }
        public int?    ParentId        { get; set; }
        public int?    ServiceId       { get; set; }
    }
}
