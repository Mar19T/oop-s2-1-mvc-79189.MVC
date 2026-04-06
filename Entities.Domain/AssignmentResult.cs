using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Domain;

    public class AssignmentResult
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public int StudentProfileId { get; set; }
        public int Score { get; set; }
        public string Feedback { get; set; } = string.Empty;

        // Navigation
        public Assignment Assignment { get; set; } = null!;
        public StudentProfile StudentProfile { get; set; } = null!;
    }


