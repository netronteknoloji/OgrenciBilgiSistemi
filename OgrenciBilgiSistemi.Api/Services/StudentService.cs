using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class StudentService
    {
        private readonly string _connectionString;

        public StudentService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bağlantı dizesi eksik.");
        }

        public async Task<List<Student>> GetStudentsByClassIdAsync(int classId)
        {
            var students = new List<Student>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT OgrenciId, OgrenciAdSoyad, OgrenciGorsel
                    FROM Ogrenciler
                    WHERE BirimId = @classId AND OgrenciDurum = 1";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@classId", classId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string rawFileName = reader["OgrenciGorsel"]?.ToString() ?? string.Empty;
                    students.Add(new Student
                    {
                        Id       = (int)reader["OgrenciId"],
                        FullName = reader["OgrenciAdSoyad"]?.ToString() ?? string.Empty,
                        ImagePath = string.IsNullOrEmpty(rawFileName) ? "user_icon.png" : rawFileName
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Öğrenci listesi alınamadı.", ex);
            }
            return students;
        }

        public async Task<Student?> GetStudentByIdAsync(int studentId)
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT OgrenciId, OgrenciAdSoyad, OgrenciGorsel, BirimId, ParentId
                    FROM Ogrenciler
                    WHERE OgrenciId = @studentId AND OgrenciDurum = 1";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@studentId", studentId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Student
                    {
                        Id        = (int)reader["OgrenciId"],
                        FullName  = reader["OgrenciAdSoyad"]?.ToString() ?? string.Empty,
                        ImagePath = reader["OgrenciGorsel"]?.ToString(),
                        UnitId    = reader["BirimId"]   as int?,
                        ParentId  = reader["ParentId"]  as int?
                    };
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Öğrenci bilgisi alınamadı.", ex);
            }
            return null;
        }

        public async Task<Dictionary<int, int>> GetExistingAttendanceAsync(int classId, int lessonNumber)
        {
            if (lessonNumber < 1 || lessonNumber > 8)
                throw new ArgumentOutOfRangeException(nameof(lessonNumber), "Ders numarası 1-8 arasında olmalıdır.");

            var attendanceDict = new Dictionary<int, int>();
            string dersColumn = $"Ders{lessonNumber}";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                string query = $@"
                    SELECT SY.OgrenciId, SY.{dersColumn}
                    FROM SinifYoklama SY
                    INNER JOIN Ogrenciler O ON SY.OgrenciId = O.OgrenciId
                    WHERE O.BirimId = @classId
                      AND CAST(SY.OlusturulmaTarihi AS DATE) = CAST(GETDATE() AS DATE)";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@classId", classId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (reader[dersColumn] != DBNull.Value)
                        attendanceDict[(int)reader["OgrenciId"]] = (int)reader[dersColumn];
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Yoklama bilgisi alınamadı.", ex);
            }
            return attendanceDict;
        }

        public async Task SaveBulkAttendanceAsync(
            IEnumerable<(int StudentId, int StatusId)> attendanceData,
            int classId,
            int teacherId,
            int lessonNumber)
        {
            if (lessonNumber < 1 || lessonNumber > 8)
                throw new ArgumentOutOfRangeException(nameof(lessonNumber), "Ders numarası 1-8 arasında olmalıdır.");

            string dersColumn = $"Ders{lessonNumber}";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                string query = $@"
                    DECLARE @Bugun DATE = CAST(GETDATE() AS DATE);
                    MERGE INTO SinifYoklama AS target
                    USING (SELECT @studentId AS OgrenciId) AS source
                    ON (target.OgrenciId = source.OgrenciId AND CAST(target.OlusturulmaTarihi AS DATE) = @Bugun)
                    WHEN MATCHED THEN
                        UPDATE SET {dersColumn} = @statusId, GuncellenmeTarihi = GETDATE()
                    WHEN NOT MATCHED THEN
                        INSERT (OgrenciId, OgretmenId, {dersColumn}, OlusturulmaTarihi)
                        VALUES (@studentId, @teacherId, @statusId, GETDATE());";

                foreach (var record in attendanceData)
                {
                    await using var cmd = new SqlCommand(query, conn, transaction);
                    cmd.Parameters.AddWithValue("@studentId", record.StudentId);
                    cmd.Parameters.AddWithValue("@statusId",  record.StatusId);
                    cmd.Parameters.AddWithValue("@teacherId", teacherId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch (SqlException ex)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Yoklama kaydedilemedi.", ex);
            }
        }

        public async Task<Dictionary<string, string>> GetStudentFullDetailsAsync(int studentId)
        {
            var details = new Dictionary<string, string>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT
                        s.OgrenciAdSoyad, s.OgrenciNo, s.OgrenciKartNo, s.OgrenciGorsel,
                        u.BirimAd,
                        p.VeliAdSoyad, p.VeliTelefon, p.VeliEmail, p.VeliMeslek, p.VeliIsYeri, p.VeliAdres,
                        t.OgretmenAdSoyad, srv.Plaka
                    FROM Ogrenciler s
                    LEFT JOIN Birimler          u   ON s.BirimId        = u.BirimId
                    LEFT JOIN OgrenciVeliler    p   ON s.OgrenciVeliId  = p.OgrenciVeliId
                    LEFT JOIN Ogretmenler       t   ON s.OgretmenId     = t.OgretmenId
                    LEFT JOIN Servisler         srv ON srv.ServisId     = s.ServisId
                    WHERE s.OgrenciId = @studentId";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@studentId", studentId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string rawFileName = reader["OgrenciGorsel"]?.ToString() ?? string.Empty;

                    details["StudentName"]  = reader["OgrenciAdSoyad"]?.ToString()    ?? "Bilinmiyor";
                    details["StudentNo"]    = reader["OgrenciNo"]?.ToString()          ?? "-";
                    details["CardNo"]       = reader["OgrenciKartNo"]?.ToString()      ?? "-";
                    details["ImagePath"]    = string.IsNullOrEmpty(rawFileName)
                                                ? "user_icon.png"
                                                : rawFileName.Trim().ToLower();
                    details["ClassName"]    = reader["BirimAd"]?.ToString()            ?? "Atanmamış";
                    details["ParentName"]   = reader["VeliAdSoyad"]?.ToString()        ?? "Belirtilmemiş";
                    details["ParentPhone"]  = reader["VeliTelefon"]?.ToString()        ?? "-";
                    details["ParentEmail"]  = reader["VeliEmail"]?.ToString()          ?? "-";
                    details["ParentJob"]    = reader["VeliMeslek"]?.ToString()         ?? "-";
                    details["ParentWork"]   = reader["VeliIsYeri"]?.ToString()         ?? "-";
                    details["Address"]      = reader["VeliAdres"]?.ToString()          ?? "-";
                    details["TeacherName"]  = reader["OgretmenAdSoyad"]?.ToString()   ?? "Atanmamış";
                    details["PlateNumber"]  = reader["Plaka"]?.ToString()              ?? "Kullanmıyor";
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Öğrenci detayları alınamadı.", ex);
            }
            return details;
        }

        public async Task<List<ClassAttendance>> GetStudentWeeklyAttendanceAsync(
            int studentId, DateTime start, DateTime end)
        {
            var records = new List<ClassAttendance>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT
                        SinifYoklamaId, OgrenciId, OgretmenId,
                        Ders1, Ders2, Ders3, Ders4, Ders5, Ders6, Ders7, Ders8,
                        OlusturulmaTarihi
                    FROM SinifYoklama
                    WHERE OgrenciId = @studentId
                      AND CAST(OlusturulmaTarihi AS DATE) >= @start
                      AND CAST(OlusturulmaTarihi AS DATE) <= @end
                    ORDER BY OlusturulmaTarihi ASC";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@studentId", studentId);
                cmd.Parameters.AddWithValue("@start",     start.Date);
                cmd.Parameters.AddWithValue("@end",       end.Date);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    records.Add(new ClassAttendance
                    {
                        AttendanceId = (int)reader["SinifYoklamaId"],
                        StudentId    = (int)reader["OgrenciId"],
                        TeacherId    = (int)reader["OgretmenId"],
                        Lesson1      = reader["Ders1"] as int?,
                        Lesson2      = reader["Ders2"] as int?,
                        Lesson3      = reader["Ders3"] as int?,
                        Lesson4      = reader["Ders4"] as int?,
                        Lesson5      = reader["Ders5"] as int?,
                        Lesson6      = reader["Ders6"] as int?,
                        Lesson7      = reader["Ders7"] as int?,
                        Lesson8      = reader["Ders8"] as int?,
                        CreatedAt    = Convert.ToDateTime(reader["OlusturulmaTarihi"])
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Haftalık yoklama alınamadı.", ex);
            }
            return records;
        }
    }
}
