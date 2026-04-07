using Entities.Domain;
using Microsoft.EntityFrameworkCore;
using oop_s2_1_mvc_79189.Data;

namespace VgcCollege.Tests
{
    public class tests
    {
        // ── Helper — fresh in-memory database ─────────────────
        private AppDbContext GetDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // ── Test 1 — Student only sees their own results ───────
        [Fact]
        public async Task Student_OnlySeesTheirOwnAssignmentResults()
        {
            var db = GetDb();

            var student1 = new StudentProfile { Id = 1, IdentityUserId = "user1", Name = "Alice", Email = "a@a.com", Phone = "123", Address = "Addr", StudentNumber = "VGC001" };
            var student2 = new StudentProfile { Id = 2, IdentityUserId = "user2", Name = "Bob", Email = "b@b.com", Phone = "456", Address = "Addr", StudentNumber = "VGC002" };
            var branch = new Branch { Id = 1, Name = "Dublin", Address = "Dublin" };
            var course = new Course { Id = 1, Name = "Software Dev", BranchId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(6) };
            var assignment = new Assignment { Id = 1, CourseId = 1, Title = "Task 1", MaxScore = 100, DueDate = DateTime.Today };

            db.StudentProfiles.AddRange(student1, student2);
            db.Branches.Add(branch);
            db.Courses.Add(course);
            db.Assignments.Add(assignment);
            db.AssignmentResults.AddRange(
                new AssignmentResult { Id = 1, AssignmentId = 1, StudentProfileId = 1, Score = 85, Feedback = "Good" },
                new AssignmentResult { Id = 2, AssignmentId = 1, StudentProfileId = 2, Score = 72, Feedback = "OK" }
            );
            await db.SaveChangesAsync();

            // ── ACT — filter results for student1 only ─────────
            var results = await db.AssignmentResults
                .Where(r => r.StudentProfileId == student1.Id)
                .ToListAsync();

            // ── ASSERT ─────────────────────────────────────────
            Assert.Single(results);
            Assert.Equal(85, results[0].Score);
        }

        // ── Test 2 — Unreleased exam hidden from students ──────
        [Fact]
        public async Task Student_CannotSeeUnreleasedExamResults()
        {
            var db = GetDb();

            var branch = new Branch { Id = 1, Name = "Dublin", Address = "Dublin" };
            var course = new Course { Id = 1, Name = "Software Dev", BranchId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(6) };
            var releasedExam = new Exam { Id = 1, CourseId = 1, Title = "Midterm", Date = DateTime.Today, MaxScore = 100, ResultsReleased = true };
            var unreleasedExam = new Exam { Id = 2, CourseId = 1, Title = "Final", Date = DateTime.Today, MaxScore = 100, ResultsReleased = false };

            db.Branches.Add(branch);
            db.Courses.Add(course);
            db.Exams.AddRange(releasedExam, unreleasedExam);
            await db.SaveChangesAsync();

            // ── ACT — simulate what student sees ───────────────
            var visibleExams = await db.Exams
                .Where(e => e.ResultsReleased)
                .ToListAsync();

            // ── ASSERT ─────────────────────────────────────────
            Assert.Single(visibleExams);
            Assert.True(visibleExams[0].ResultsReleased);
        }

        // ── Test 3 — Duplicate enrolment is caught ─────────────
        [Fact]
        public async Task Enrolment_DuplicateIsDetected()
        {
            var db = GetDb();

            var student = new StudentProfile { Id = 1, IdentityUserId = "user1", Name = "Alice", Email = "a@a.com", Phone = "123", Address = "Addr", StudentNumber = "VGC001" };
            var branch = new Branch { Id = 1, Name = "Dublin", Address = "Dublin" };
            var course = new Course { Id = 1, Name = "Software Dev", BranchId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(6) };
            var existing = new CourseEnrolment { Id = 1, StudentProfileId = 1, CourseId = 1, EnrolDate = DateTime.Today, Status = EnrolmentStatus.Active };

            db.StudentProfiles.Add(student);
            db.Branches.Add(branch);
            db.Courses.Add(course);
            db.CourseEnrolments.Add(existing);
            await db.SaveChangesAsync();

            // ── ACT — check if duplicate exists ────────────────
            var isDuplicate = await db.CourseEnrolments
                .AnyAsync(e => e.StudentProfileId == 1 && e.CourseId == 1);

            // ── ASSERT ─────────────────────────────────────────
            Assert.True(isDuplicate);
        }

        // ── Test 4 — Score cannot exceed MaxScore ──────────────
        [Fact]
        public void Score_CannotExceedMaxScore()
        {
            var assignment = new Assignment { MaxScore = 100 };
            var result = new AssignmentResult { Score = 110 };

            // ── ACT ────────────────────────────────────────────
            bool isValid = result.Score <= assignment.MaxScore;

            // ── ASSERT ─────────────────────────────────────────
            Assert.False(isValid);
        }

        // ── Test 5 — Grade auto calculation ────────────────────
        [Fact]
        public void Grade_IsCalculatedCorrectly()
        {
            // ── ACT & ASSERT ───────────────────────────────────
            Assert.Equal("A", CalculateGrade(85, 100));
            Assert.Equal("B", CalculateGrade(60, 100));
            Assert.Equal("C", CalculateGrade(45, 100));
            Assert.Equal("F", CalculateGrade(30, 100));
        }

        private string CalculateGrade(int score, int maxScore)
        {
            var percentage = score * 100 / maxScore;
            return percentage >= 70 ? "A"
                : percentage >= 55 ? "B"
                : percentage >= 40 ? "C" : "F";
        }

        // ── Test 6 — Faculty only sees their courses ───────────
        [Fact]
        public async Task Faculty_OnlySeesTheirCourseAttendance()
        {
            var db = GetDb();

            var branch = new Branch { Id = 1, Name = "Dublin", Address = "Dublin" };
            var course1 = new Course { Id = 1, Name = "Software Dev", BranchId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(6) };
            var course2 = new Course { Id = 2, Name = "Data Science", BranchId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(6) };

            var faculty = new FacultyProfile { Id = 1, IdentityUserId = "faculty1", Name = "Dr Smith", Email = "f@f.com", Phone = "789" };
            var assignment = new FacultyCourseAssignment { Id = 1, FacultyProfileId = 1, CourseId = 1 };

            var student = new StudentProfile { Id = 1, IdentityUserId = "user1", Name = "Alice", Email = "a@a.com", Phone = "123", Address = "Addr", StudentNumber = "VGC001" };
            var enrolment1 = new CourseEnrolment { Id = 1, StudentProfileId = 1, CourseId = 1, EnrolDate = DateTime.Today, Status = EnrolmentStatus.Active };
            var enrolment2 = new CourseEnrolment { Id = 2, StudentProfileId = 1, CourseId = 2, EnrolDate = DateTime.Today, Status = EnrolmentStatus.Active };

            db.Branches.Add(branch);
            db.Courses.AddRange(course1, course2);
            db.FacultyProfiles.Add(faculty);
            db.FacultyCourseAssignments.Add(assignment);
            db.StudentProfiles.Add(student);
            db.CourseEnrolments.AddRange(enrolment1, enrolment2);
            db.AttendanceRecords.AddRange(
                new AttendanceRecord { Id = 1, CourseEnrolmentId = 1, WeekNumber = 1, Date = DateTime.Today, Present = true },
                new AttendanceRecord { Id = 2, CourseEnrolmentId = 2, WeekNumber = 1, Date = DateTime.Today, Present = false }
            );
            await db.SaveChangesAsync();

            // ── ACT — get only faculty's course IDs ────────────
            var facultyCourseIds = await db.FacultyCourseAssignments
                .Where(f => f.FacultyProfileId == faculty.Id)
                .Select(f => f.CourseId)
                .ToListAsync();

            var attendance = await db.AttendanceRecords
                .Include(a => a.CourseEnrolment)
                .Where(a => facultyCourseIds.Contains(a.CourseEnrolment.CourseId))
                .ToListAsync();

            // ── ASSERT ─────────────────────────────────────────
            Assert.Single(attendance);
            Assert.Equal(1, attendance[0].CourseEnrolmentId);
        }

        // ── Test 7 — Withdrawn student is not active ───────────
        [Fact]
        public async Task WithdrawnStudent_IsNotShownAsActive()
        {
            var db = GetDb();

            var student = new StudentProfile { Id = 1, IdentityUserId = "user1", Name = "Alice", Email = "a@a.com", Phone = "123", Address = "Addr", StudentNumber = "VGC001" };
            var branch = new Branch { Id = 1, Name = "Dublin", Address = "Dublin" };
            var course = new Course { Id = 1, Name = "Software Dev", BranchId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(6) };

            db.StudentProfiles.Add(student);
            db.Branches.Add(branch);
            db.Courses.Add(course);
            db.CourseEnrolments.AddRange(
                new CourseEnrolment { Id = 1, StudentProfileId = 1, CourseId = 1, EnrolDate = DateTime.Today, Status = EnrolmentStatus.Active },
                new CourseEnrolment { Id = 2, StudentProfileId = 1, CourseId = 1, EnrolDate = DateTime.Today, Status = EnrolmentStatus.Withdrawn }
            );
            await db.SaveChangesAsync();

            // ── ACT ────────────────────────────────────────────
            var activeEnrolments = await db.CourseEnrolments
                .Where(e => e.Status == EnrolmentStatus.Active)
                .ToListAsync();

            // ── ASSERT ─────────────────────────────────────────
            Assert.Single(activeEnrolments);
            Assert.Equal(EnrolmentStatus.Active, activeEnrolments[0].Status);
        }

        // ── Test 8 — Admin can release exam results ────────────
        [Fact]
        public async Task Admin_CanReleaseExamResults()
        {
            var db = GetDb();

            var branch = new Branch { Id = 1, Name = "Dublin", Address = "Dublin" };
            var course = new Course { Id = 1, Name = "Software Dev", BranchId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(6) };
            var exam = new Exam { Id = 1, CourseId = 1, Title = "Final", Date = DateTime.Today, MaxScore = 100, ResultsReleased = false };

            db.Branches.Add(branch);
            db.Courses.Add(course);
            db.Exams.Add(exam);
            await db.SaveChangesAsync();

            // ── ACT — simulate release ─────────────────────────
            exam.ResultsReleased = true;
            await db.SaveChangesAsync();

            // ── ASSERT ─────────────────────────────────────────
            var updated = await db.Exams.FindAsync(1);
            Assert.True(updated!.ResultsReleased);
        }
    }
}