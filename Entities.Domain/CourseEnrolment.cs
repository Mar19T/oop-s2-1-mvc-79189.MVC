using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Domain;

    public enum EnrolmentStatus { Active, Withdrawn, Completed }
    public class CourseEnrolment
    {
        public int Id { get; set; }
        public int StudentProfileId { get; set; }
        public int CourseId { get; set; }
        public DateTime EnrolDate { get; set; }
        public EnrolmentStatus Status { get; set; }

        // Navigation
        public StudentProfile StudentProfile { get; set; } = null!;
        public Course Course { get; set; } = null!;
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }

    

