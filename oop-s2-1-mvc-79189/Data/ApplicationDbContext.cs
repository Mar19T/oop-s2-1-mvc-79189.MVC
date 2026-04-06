using Entities.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace oop_s2_1_mvc_79189.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Branch> Branches { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<FacultyProfile> FacultyProfiles { get; set; }
        public DbSet<FacultyCourseAssignment> FacultyCourseAssignments { get; set; }
        public DbSet<CourseEnrolment> CourseEnrolments { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<AssignmentResult> AssignmentResults { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Branch -> Courses
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Branch)
                .WithMany(b => b.Courses)
                .HasForeignKey(c => c.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Course -> Enrolments
            modelBuilder.Entity<CourseEnrolment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // StudentProfile -> Enrolments
            modelBuilder.Entity<CourseEnrolment>()
                .HasOne(e => e.StudentProfile)
                .WithMany(s => s.Enrolments)
                .HasForeignKey(e => e.StudentProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // CourseEnrolment -> AttendanceRecords
            modelBuilder.Entity<AttendanceRecord>()
                .HasOne(a => a.CourseEnrolment)
                .WithMany(e => e.AttendanceRecords)
                .HasForeignKey(a => a.CourseEnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Course -> Assignments
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Course)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Assignment -> AssignmentResults
            modelBuilder.Entity<AssignmentResult>()
                .HasOne(r => r.Assignment)
                .WithMany(a => a.Results)
                .HasForeignKey(r => r.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // StudentProfile -> AssignmentResults
            modelBuilder.Entity<AssignmentResult>()
                .HasOne(r => r.StudentProfile)
                .WithMany(s => s.AssignmentResults)
                .HasForeignKey(r => r.StudentProfileId)
                .OnDelete(DeleteBehavior.NoAction);

            // Course -> Exams
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Exams)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Exam -> ExamResults
            modelBuilder.Entity<ExamResult>()
                .HasOne(r => r.Exam)
                .WithMany(e => e.Results)
                .HasForeignKey(r => r.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // StudentProfile -> ExamResults
            modelBuilder.Entity<ExamResult>()
                .HasOne(r => r.StudentProfile)
                .WithMany(s => s.ExamResults)
                .HasForeignKey(r => r.StudentProfileId)
                .OnDelete(DeleteBehavior.NoAction);

            // FacultyProfile -> FacultyCourseAssignments
            modelBuilder.Entity<FacultyCourseAssignment>()
                .HasOne(f => f.FacultyProfile)
                .WithMany(fp => fp.CourseAssignments)
                .HasForeignKey(f => f.FacultyProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Course -> FacultyCourseAssignments
            modelBuilder.Entity<FacultyCourseAssignment>()
                .HasOne(f => f.Course)
                .WithMany(c => c.FacultyAssignments)
                .HasForeignKey(f => f.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Store enums as strings
            modelBuilder.Entity<CourseEnrolment>()
                .Property(e => e.Status)
                .HasConversion<string>();
        }
    }
}
