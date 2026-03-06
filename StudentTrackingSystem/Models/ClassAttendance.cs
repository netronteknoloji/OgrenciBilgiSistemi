using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentTrackingSystem.Models
{
    public class ClassAttendance
    {
        public int AttendanceId { get; set; }
        public int? Lesson1 { get; set; }
        public int? Lesson2 { get; set; }
        public int? Lesson3 { get; set; }
        public int? Lesson4 { get; set; }
        public int? Lesson5 { get; set; }
        public int? Lesson6 { get; set; }
        public int? Lesson7 { get; set; }
        public int? Lesson8 { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int StudentId { get; set; }
        public int TeacherId { get; set; }

        /// <summary>
        /// Ders numarasına göre (1-8) yoklama durumunu döner. Reflection kullanımını önler.
        /// </summary>
        public int? GetLesson(int lessonNumber) => lessonNumber switch
        {
            1 => Lesson1,
            2 => Lesson2,
            3 => Lesson3,
            4 => Lesson4,
            5 => Lesson5,
            6 => Lesson6,
            7 => Lesson7,
            8 => Lesson8,
            _ => null
        };
    }
}
