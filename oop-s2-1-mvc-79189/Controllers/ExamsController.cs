using Entities.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using oop_s2_1_mvc_79189.Data;

namespace oop_s2_1_mvc_79189.Controllers
{
    [Authorize]
    public class ExamsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ExamsController> _logger;

        public ExamsController(AppDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<ExamsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ── Exams List ─────────────────────────────────────────
        [Authorize(Roles = "Administrator,Faculty,Student")]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Administrator"))
            {
                var all = await _context.Exams
                    .Include(e => e.Course)
                    .Include(e => e.Results)
                    .ToListAsync();
                return View(all);
            }

            if (User.IsInRole("Faculty"))
            {
                var userId = _userManager.GetUserId(User);
                var faculty = await _context.FacultyProfiles
                    .Include(f => f.CourseAssignments)
                    .FirstOrDefaultAsync(f => f.IdentityUserId == userId);

                if (faculty == null) return View(new List<Exam>());

                var courseIds = faculty.CourseAssignments.Select(ca => ca.CourseId);
                var exams = await _context.Exams
                    .Include(e => e.Course)
                    .Include(e => e.Results)
                    .Where(e => courseIds.Contains(e.CourseId))
                    .ToListAsync();
                return View(exams);
            }

            // ✅ Student — only sees exams for their courses
            var studentUserId = _userManager.GetUserId(User);
            var student = await _context.StudentProfiles
                .Include(s => s.Enrolments)
                .FirstOrDefaultAsync(s => s.IdentityUserId == studentUserId);

            if (student == null) return View(new List<Exam>());

            var enrolledCourseIds = student.Enrolments.Select(e => e.CourseId);
            var myExams = await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Results)
                .Where(e => enrolledCourseIds.Contains(e.CourseId))
                .ToListAsync();
            return View(myExams);
        }

        // ── Create Exam ────────────────────────────────────────
        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(
                _context.Courses.Select(c => new { c.Id, c.Name }),
                "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create([Bind("Id,CourseId,Title,Date,MaxScore,ResultsReleased")] Exam exam)
        {
            ModelState.Remove("Course");
            ModelState.Remove("Results");

            if (ModelState.IsValid)
            {
                _context.Add(exam);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Exam created: {Title}", exam.Title);
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(
                _context.Courses.Select(c => new { c.Id, c.Name }),
                "Id", "Name", exam.CourseId);
            return View(exam);
        }

        // ── Edit Exam ──────────────────────────────────────────
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            ViewData["CourseId"] = new SelectList(
                _context.Courses.Select(c => new { c.Id, c.Name }),
                "Id", "Name", exam.CourseId);
            return View(exam);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CourseId,Title,Date,MaxScore,ResultsReleased")] Exam exam)
        {
            if (id != exam.Id) return NotFound();

            ModelState.Remove("Course");
            ModelState.Remove("Results");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(exam);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Exam {Id} updated, ResultsReleased {Released}",
                        exam.Id, exam.ResultsReleased);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Exams.Any(e => e.Id == exam.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(
                _context.Courses.Select(c => new { c.Id, c.Name }),
                "Id", "Name", exam.CourseId);
            return View(exam);
        }

        // ── Release Results (Admin only) ───────────────────────
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ReleaseResults(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            exam.ResultsReleased = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Exam {Id} results released by {User}",
                id, User.Identity?.Name);

            return RedirectToAction(nameof(Index));
        }

        // ── Delete Exam ────────────────────────────────────────
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null) return NotFound();
            return View(exam);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam != null)
            {
                _context.Exams.Remove(exam);
                _logger.LogInformation("Exam {Id} deleted", id);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ── Exam Results ───────────────────────────────────────
        [Authorize(Roles = "Administrator,Faculty,Student")]
        public async Task<IActionResult> Results(int? examId)
        {
            if (examId == null) return NotFound();

            var exam = await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Results)
                    .ThenInclude(r => r.StudentProfile)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null) return NotFound();

            // ✅ Students cannot see provisional results
            if (User.IsInRole("Student") && !exam.ResultsReleased)
            {
                _logger.LogWarning(
                    "Student {User} attempted to view unreleased exam results for Exam {ExamId}",
                    User.Identity?.Name, examId);
                return View("ResultsNotReleased", exam);
            }

            // ✅ Student only sees their own result
            if (User.IsInRole("Student"))
            {
                var userId = _userManager.GetUserId(User);
                var student = await _context.StudentProfiles
                    .FirstOrDefaultAsync(s => s.IdentityUserId == userId);

                exam.Results = exam.Results
                    .Where(r => r.StudentProfileId == student!.Id)
                    .ToList();
            }

            return View(exam);
        }

        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> AddResult(int? examId)
        {
            if (examId == null) return NotFound();

            var exam = await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null) return NotFound();

            ViewBag.Exam = exam;
            ViewData["StudentProfileId"] = new SelectList(
                _context.CourseEnrolments
                    .Include(e => e.StudentProfile)
                    .Where(e => e.CourseId == exam.CourseId
                             && e.Status == EnrolmentStatus.Active)
                    .Select(e => new { e.StudentProfile.Id, e.StudentProfile.Name }),
                "Id", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> AddResult([Bind("Id,ExamId,StudentProfileId,Score,Grade")] ExamResult result)
        {
            ModelState.Remove("Exam");
            ModelState.Remove("StudentProfile");

            var exam = await _context.Exams.FindAsync(result.ExamId);
            if (exam != null && result.Score > exam.MaxScore)
            {
                ModelState.AddModelError("Score",
                    $"Score cannot exceed the maximum of {exam.MaxScore}.");
            }

            if (ModelState.IsValid)
            {
                // ✅ Auto calculate grade if not provided
                if (string.IsNullOrEmpty(result.Grade))
                {
                    var percentage = exam != null
                        ? result.Score * 100 / exam.MaxScore : 0;
                    result.Grade = percentage >= 70 ? "A"
                        : percentage >= 55 ? "B"
                        : percentage >= 40 ? "C" : "F";
                }

                _context.Add(result);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Exam result added for ExamId {ExamId}, StudentId {StudentId}, Score {Score}, Grade {Grade}",
                    result.ExamId, result.StudentProfileId, result.Score, result.Grade);

                return RedirectToAction(nameof(Results), new { examId = result.ExamId });
            }

            exam = await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == result.ExamId);

            ViewBag.Exam = exam;
            ViewData["StudentProfileId"] = new SelectList(
                _context.CourseEnrolments
                    .Include(e => e.StudentProfile)
                    .Where(e => e.CourseId == exam!.CourseId
                             && e.Status == EnrolmentStatus.Active)
                    .Select(e => new { e.StudentProfile.Id, e.StudentProfile.Name }),
                "Id", "Name", result.StudentProfileId);

            return View(result);
        }

        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> EditResult(int? id)
        {
            if (id == null) return NotFound();

            var result = await _context.ExamResults
                .Include(r => r.Exam)
                .Include(r => r.StudentProfile)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (result == null) return NotFound();

            ViewBag.StudentName = result.StudentProfile?.Name;
            ViewBag.ExamTitle = result.Exam?.Title;
            ViewBag.MaxScore = result.Exam?.MaxScore;

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> EditResult(int id, [Bind("Id,ExamId,StudentProfileId,Score,Grade")] ExamResult result)
        {
            if (id != result.Id) return NotFound();

            ModelState.Remove("Exam");
            ModelState.Remove("StudentProfile");

            var exam = await _context.Exams.FindAsync(result.ExamId);
            if (exam != null && result.Score > exam.MaxScore)
            {
                ModelState.AddModelError("Score",
                    $"Score cannot exceed the maximum of {exam.MaxScore}.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(result);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Exam result {Id} updated", result.Id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ExamResults.Any(r => r.Id == result.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Results), new { examId = result.ExamId });
            }

            var existing = await _context.ExamResults
                .Include(r => r.Exam)
                .Include(r => r.StudentProfile)
                .FirstOrDefaultAsync(r => r.Id == id);

            ViewBag.StudentName = existing?.StudentProfile?.Name;
            ViewBag.ExamTitle = existing?.Exam?.Title;
            ViewBag.MaxScore = existing?.Exam?.MaxScore;

            return View(result);
        }
    }
}