using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Domain;

    public class FacultyCourseAssignment
    {
        public int Id { get; set; }
        public int FacultyProfileId { get; set; }
        public int CourseId { get; set; }

        // Navigation
        public FacultyProfile FacultyProfile { get; set; } = null!;
        public Course Course { get; set; } = null!;
    }

