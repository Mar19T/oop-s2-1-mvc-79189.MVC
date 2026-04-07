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
    public class GradebookController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<GradebookController> _logger;

        public GradebookController(AppDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<GradebookController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ── Assignments ────────────────────────────────────────
        [Authorize(Roles = "Administrator,Faculty,Student")]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Administrator"))
            {
                var all = await _context.Assignments
                    .Include(a => a.Course)
                    .Include(a => a.Results)
                    .ToListAsync();
                return View(all);
            }

            if (User.IsInRole("Faculty"))
            {
                var userId = _userManager.GetUserId(User);
                var faculty = await _context.FacultyProfiles
                    .Include(f => f.CourseAssignments)
                    .FirstOrDefaultAsync(f => f.IdentityUserId == userId);

                if (faculty == null) return View(new List<Assignment>());

                var courseIds = faculty.CourseAssignments.Select(ca => ca.CourseId);
                var assignments = await _context.Assignments
                    .Include(a => a.Course)
                    .Include(a => a.Results)
                    .Where(a => courseIds.Contains(a.CourseId))
                    .ToListAsync();
                return View(assignments);
            }

            // Student — assignments for their enrolled courses
            var studentUserId = _userManager.GetUserId(User);
            var student = await _context.StudentProfiles
                .Include(s => s.Enrolments)
                .FirstOrDefaultAsync(s => s.IdentityUserId == studentUserId);

            if (student == null) return View(new List<Assignment>());

            var enrolledCourseIds = student.Enrolments.Select(e => e.CourseId);
            var myAssignments = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Results)
                .Where(a => enrolledCourseIds.Contains(a.CourseId))
                .ToListAsync();
            return View(myAssignments);
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult CreateAssignment()
        {
            ViewData["CourseId"] = new SelectList(
                _context.Courses.Select(c => new { c.Id, c.Name }),
                "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> CreateAssignment([Bind("Id,CourseId,Title,MaxScore,DueDate")] Assignment assignment)
        {
            ModelState.Remove("Course");
            ModelState.Remove("Results");

            if (ModelState.IsValid)
            {
                _context.Add(assignment);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Assignment created: {Title}", assignment.Title);
                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseId"] = new SelectList(
                _context.Courses.Select(c => new { c.Id, c.Name }),
                "Id", "Name", assignment.CourseId);
            return View(assignment);
        }

        // ── Assignment Results ─────────────────────────────────
        [Authorize(Roles = "Administrator,Faculty,Student")]
        public async Task<IActionResult> Results(int? assignmentId)
        {
            if (assignmentId == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Results)
                    .ThenInclude(r => r.StudentProfile)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null) return NotFound();

            // ✅ Student only sees their own result
            if (User.IsInRole("Student"))
            {
                var userId = _userManager.GetUserId(User);
                var student = await _context.StudentProfiles
                    .FirstOrDefaultAsync(s => s.IdentityUserId == userId);

                assignment.Results = assignment.Results
                    .Where(r => r.StudentProfileId == student!.Id)
                    .ToList();
            }

            return View(assignment);
        }

        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> AddResult(int? assignmentId)
        {
            if (assignmentId == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null) return NotFound();

            ViewBag.Assignment = assignment;
            ViewData["StudentProfileId"] = new SelectList(
                _context.CourseEnrolments
                    .Include(e => e.StudentProfile)
                    .Where(e => e.CourseId == assignment.CourseId
                             && e.Status == EnrolmentStatus.Active)
                    .Select(e => new { e.StudentProfile.Id, e.StudentProfile.Name }),
                "Id", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> AddResult([Bind("Id,AssignmentId,StudentProfileId,Score,Feedback")] AssignmentResult result)
        {
            ModelState.Remove("Assignment");
            ModelState.Remove("StudentProfile");

            // ✅ Score cannot exceed MaxScore
            var assignment = await _context.Assignments.FindAsync(result.AssignmentId);
            if (assignment != null && result.Score > assignment.MaxScore)
            {
                ModelState.AddModelError("Score",
                    $"Score cannot exceed the maximum of {assignment.MaxScore}.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(result);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Result added for AssignmentId {AssignmentId}, StudentId {StudentId}, Score {Score}",
                    result.AssignmentId, result.StudentProfileId, result.Score);

                return RedirectToAction(nameof(Results), new { assignmentId = result.AssignmentId });
            }

            assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == result.AssignmentId);

            ViewBag.Assignment = assignment;
            ViewData["StudentProfileId"] = new SelectList(
                _context.CourseEnrolments
                    .Include(e => e.StudentProfile)
                    .Where(e => e.CourseId == assignment!.CourseId
                             && e.Status == EnrolmentStatus.Active)
                    .Select(e => new { e.StudentProfile.Id, e.StudentProfile.Name }),
                "Id", "Name", result.StudentProfileId);

            return View(result);
        }

        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> EditResult(int? id)
        {
            if (id == null) return NotFound();

            var result = await _context.AssignmentResults
                .Include(r => r.Assignment)
                .Include(r => r.StudentProfile)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (result == null) return NotFound();

            ViewBag.StudentName = result.StudentProfile?.Name;
            ViewBag.AssignmentTitle = result.Assignment?.Title;
            ViewBag.MaxScore = result.Assignment?.MaxScore;

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> EditResult(int id, [Bind("Id,AssignmentId,StudentProfileId,Score,Feedback")] AssignmentResult result)
        {
            if (id != result.Id) return NotFound();

            ModelState.Remove("Assignment");
            ModelState.Remove("StudentProfile");

            var assignment = await _context.Assignments.FindAsync(result.AssignmentId);
            if (assignment != null && result.Score > assignment.MaxScore)
            {
                ModelState.AddModelError("Score",
                    $"Score cannot exceed the maximum of {assignment.MaxScore}.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(result);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Result {Id} updated, Score {Score}", result.Id, result.Score);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.AssignmentResults.Any(r => r.Id == result.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Results), new { assignmentId = result.AssignmentId });
            }

            var existing = await _context.AssignmentResults
                .Include(r => r.Assignment)
                .Include(r => r.StudentProfile)
                .FirstOrDefaultAsync(r => r.Id == id);

            ViewBag.StudentName = existing?.StudentProfile?.Name;
            ViewBag.AssignmentTitle = existing?.Assignment?.Title;
            ViewBag.MaxScore = existing?.Assignment?.MaxScore;

            return View(result);
        }
    }
}