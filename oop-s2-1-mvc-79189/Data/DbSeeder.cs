using Bogus;
using Entities.Domain;
using Microsoft.AspNetCore.Identity;
using oop_s2_1_mvc_79189.Models;

using System;

namespace oop_s2_1_mvc_79189.Data
{
    public class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var db = services.GetRequiredService<AppDbContext>();

            // ── Roles ──────────────────────────────────────────
            string[] roles = ["Administrator", "Faculty", "Student"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ── Admin user ─────────────────────────────────────
            await CreateUser(userManager, "admin@vgc.ie", "Admin123!", "Administrator");

            // ── Stop here if already seeded ────────────────────
            if (db.Branches.Any()) return;

            // ── Branches ───────────────────────────────────────
            var branches = new List<Branch>
            {
                new() { Name = "Dublin Campus",  Address = "1 O'Connell St, Dublin" },
                new() { Name = "Cork Campus",    Address = "10 Patrick St, Cork" },
                new() { Name = "Galway Campus",  Address = "5 Shop St, Galway" }
            };
            db.Branches.AddRange(branches);
            await db.SaveChangesAsync();

            // ── Courses ────────────────────────────────────────
            var now = DateTime.Today;
            var courses = new List<Course>
            {
                new() { Name = "Software Development",  BranchId = branches[0].Id, StartDate = now.AddMonths(-3), EndDate = now.AddMonths(6) },
                new() { Name = "Data Science",          BranchId = branches[0].Id, StartDate = now.AddMonths(-2), EndDate = now.AddMonths(7) },
                new() { Name = "Cybersecurity",         BranchId = branches[1].Id, StartDate = now.AddMonths(-1), EndDate = now.AddMonths(8) },
                new() { Name = "Web Design",            BranchId = branches[1].Id, StartDate = now.AddMonths(-4), EndDate = now.AddMonths(5) },
                new() { Name = "Cloud Computing",       BranchId = branches[2].Id, StartDate = now.AddMonths(-2), EndDate = now.AddMonths(7) },
                new() { Name = "AI & Machine Learning", BranchId = branches[2].Id, StartDate = now.AddMonths(-1), EndDate = now.AddMonths(9) },
            };
            db.Courses.AddRange(courses);
            await db.SaveChangesAsync();

            // ── Faculty users + profiles ───────────────────────
            var facultyData = new[]
            {
                new { Email = "faculty1@vgc.ie", Name = "Dr. Sarah Murphy",  Phone = "085-1234567" },
                new { Email = "faculty2@vgc.ie", Name = "Prof. James Kelly", Phone = "086-2345678" },
                new { Email = "faculty3@vgc.ie", Name = "Dr. Emma Walsh",    Phone = "087-3456789" },
            };

            var facultyProfiles = new List<FacultyProfile>();
            foreach (var f in facultyData)
            {
                var user = await CreateUser(userManager, f.Email, "Faculty123!", "Faculty");
                if (user != null)
                {
                    var profile = new FacultyProfile
                    {
                        IdentityUserId = user.Id,
                        Name = f.Name,
                        Email = f.Email,
                        Phone = f.Phone
                    };
                    db.FacultyProfiles.Add(profile);
                    facultyProfiles.Add(profile);
                }
            }
            await db.SaveChangesAsync();

            // ── Faculty course assignments ─────────────────────
            var facultyAssignments = new List<FacultyCourseAssignment>
            {
                new() { FacultyProfileId = facultyProfiles[0].Id, CourseId = courses[0].Id },
                new() { FacultyProfileId = facultyProfiles[0].Id, CourseId = courses[1].Id },
                new() { FacultyProfileId = facultyProfiles[1].Id, CourseId = courses[2].Id },
                new() { FacultyProfileId = facultyProfiles[1].Id, CourseId = courses[3].Id },
                new() { FacultyProfileId = facultyProfiles[2].Id, CourseId = courses[4].Id },
                new() { FacultyProfileId = facultyProfiles[2].Id, CourseId = courses[5].Id },
            };
            db.FacultyCourseAssignments.AddRange(facultyAssignments);
            await db.SaveChangesAsync();

            // ── Student users + profiles (Bogus) ──────────────
            var studentEmails = new[]
            {
                "student1@vgc.ie", "student2@vgc.ie", "student3@vgc.ie",
                "student4@vgc.ie", "student5@vgc.ie", "student6@vgc.ie"
            };

            var studentFaker = new Faker<StudentProfile>()
                .RuleFor(s => s.Name, f => f.Name.FullName())
                .RuleFor(s => s.Phone, f => f.Phone.PhoneNumber("08#-#######"))
                .RuleFor(s => s.Address, f => f.Address.StreetAddress())
                .RuleFor(s => s.DateOfBirth, f => f.Date.Between(
                    DateTime.Today.AddYears(-30),
                    DateTime.Today.AddYears(-18)));

            var studentProfiles = new List<StudentProfile>();
            for (int i = 0; i < studentEmails.Length; i++)
            {
                var user = await CreateUser(userManager, studentEmails[i], "Student123!", "Student");
                if (user != null)
                {
                    var profile = studentFaker.Generate();
                    profile.IdentityUserId = user.Id;
                    profile.Email = studentEmails[i];
                    profile.StudentNumber = $"VGC{2024 + i:D4}";
                    db.StudentProfiles.Add(profile);
                    studentProfiles.Add(profile);
                }
            }
            await db.SaveChangesAsync();

            // ── Enrolments ─────────────────────────────────────
            var enrolments = new List<CourseEnrolment>
            {
                // Students 0-1 in Software Development
                new() { StudentProfileId = studentProfiles[0].Id, CourseId = courses[0].Id, EnrolDate = now.AddMonths(-3), Status = EnrolmentStatus.Active },
                new() { StudentProfileId = studentProfiles[1].Id, CourseId = courses[0].Id, EnrolDate = now.AddMonths(-3), Status = EnrolmentStatus.Active },
                // Students 2-3 in Data Science
                new() { StudentProfileId = studentProfiles[2].Id, CourseId = courses[1].Id, EnrolDate = now.AddMonths(-2), Status = EnrolmentStatus.Active },
                new() { StudentProfileId = studentProfiles[3].Id, CourseId = courses[1].Id, EnrolDate = now.AddMonths(-2), Status = EnrolmentStatus.Active },
                // Students 4-5 in Cybersecurity
                new() { StudentProfileId = studentProfiles[4].Id, CourseId = courses[2].Id, EnrolDate = now.AddMonths(-1), Status = EnrolmentStatus.Active },
                new() { StudentProfileId = studentProfiles[5].Id, CourseId = courses[2].Id, EnrolDate = now.AddMonths(-1), Status = EnrolmentStatus.Active },
                // Student 0 also in Cloud Computing
                new() { StudentProfileId = studentProfiles[0].Id, CourseId = courses[4].Id, EnrolDate = now.AddMonths(-2), Status = EnrolmentStatus.Active },
                // Student 2 also in Web Design
                new() { StudentProfileId = studentProfiles[2].Id, CourseId = courses[3].Id, EnrolDate = now.AddMonths(-1), Status = EnrolmentStatus.Withdrawn },
            };
            db.CourseEnrolments.AddRange(enrolments);
            await db.SaveChangesAsync();

            // ── Attendance records ─────────────────────────────
            var random = new Random();
            var attendanceRecords = new List<AttendanceRecord>();
            foreach (var enrolment in enrolments.Where(e => e.Status == EnrolmentStatus.Active))
            {
                for (int week = 1; week <= 8; week++)
                {
                    attendanceRecords.Add(new AttendanceRecord
                    {
                        CourseEnrolmentId = enrolment.Id,
                        WeekNumber = week,
                        Date = now.AddDays(-(8 - week) * 7),
                        Present = random.Next(100) < 80 // 80% attendance rate
                    });
                }
            }
            db.AttendanceRecords.AddRange(attendanceRecords);
            await db.SaveChangesAsync();

            // ── Assignments ────────────────────────────────────
            var assignments = new List<Assignment>
            {
                new() { CourseId = courses[0].Id, Title = "Project Plan",       MaxScore = 100, DueDate = now.AddDays(-30) },
                new() { CourseId = courses[0].Id, Title = "Mid-Term Report",    MaxScore = 100, DueDate = now.AddDays(-10) },
                new() { CourseId = courses[1].Id, Title = "Data Analysis Task", MaxScore = 100, DueDate = now.AddDays(-20) },
                new() { CourseId = courses[1].Id, Title = "ML Model Report",    MaxScore = 100, DueDate = now.AddDays(-5)  },
                new() { CourseId = courses[2].Id, Title = "Security Audit",     MaxScore = 100, DueDate = now.AddDays(-15) },
            };
            db.Assignments.AddRange(assignments);
            await db.SaveChangesAsync();

            // ── Assignment results ─────────────────────────────
            var assignmentResults = new List<AssignmentResult>();
            foreach (var assignment in assignments)
            {
                var enrolledStudents = enrolments
                    .Where(e => e.CourseId == assignment.CourseId)
                    .Select(e => e.StudentProfileId);

                foreach (var studentId in enrolledStudents)
                {
                    assignmentResults.Add(new AssignmentResult
                    {
                        AssignmentId = assignment.Id,
                        StudentProfileId = studentId,
                        Score = random.Next(50, 100),
                        Feedback = new Faker().Lorem.Sentence()
                    });
                }
            }
            db.AssignmentResults.AddRange(assignmentResults);
            await db.SaveChangesAsync();

            // ── Exams ──────────────────────────────────────────
            var exams = new List<Exam>
            {
                new() { CourseId = courses[0].Id, Title = "Software Dev Midterm",  Date = now.AddDays(-15), MaxScore = 100, ResultsReleased = true  },
                new() { CourseId = courses[0].Id, Title = "Software Dev Final",    Date = now.AddDays(30),  MaxScore = 100, ResultsReleased = false },
                new() { CourseId = courses[1].Id, Title = "Data Science Midterm",  Date = now.AddDays(-10), MaxScore = 100, ResultsReleased = true  },
                new() { CourseId = courses[2].Id, Title = "Cybersecurity Midterm", Date = now.AddDays(-5),  MaxScore = 100, ResultsReleased = false },
            };
            db.Exams.AddRange(exams);
            await db.SaveChangesAsync();

            // ── Exam results ───────────────────────────────────
            var examResults = new List<ExamResult>();
            foreach (var exam in exams)
            {
                var enrolledStudents = enrolments
                    .Where(e => e.CourseId == exam.CourseId)
                    .Select(e => e.StudentProfileId);

                foreach (var studentId in enrolledStudents)
                {
                    var score = random.Next(40, 100);
                    examResults.Add(new ExamResult
                    {
                        ExamId = exam.Id,
                        StudentProfileId = studentId,
                        Score = score,
                        Grade = score >= 70 ? "A" : score >= 55 ? "B" : score >= 40 ? "C" : "F"
                    });
                }
            }
            db.ExamResults.AddRange(examResults);
            await db.SaveChangesAsync();
        }

        // ── Helper ─────────────────────────────────────────────
        private static async Task<IdentityUser?> CreateUser(UserManager<IdentityUser> userManager,
            string email, string password, string role)
        {
            if (await userManager.FindByEmailAsync(email) != null)
                return await userManager.FindByEmailAsync(email);

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                return user;
            }

            return null;
        }
    }
}
