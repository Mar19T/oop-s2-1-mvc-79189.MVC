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
    public class EnrolmentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<EnrolmentsController> _logger;

        public EnrolmentsController(AppDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<EnrolmentsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Admin sees all, Faculty sees their courses, Student sees their own
        [Authorize(Roles = "Administrator,Faculty,Student")]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Administrator"))
            {
                var all = await _context.CourseEnrolments
                    .Include(e => e.StudentProfile)
                    .Include(e => e.Course)
                    .ToListAsync();
                return View(all);
            }

            if (User.IsInRole("Faculty"))
            {
                var userId = _userManager.GetUserId(User);
                var faculty = await _context.FacultyProfiles
                    .Include(f => f.CourseAssignments)
                    .FirstOrDefaultAsync(f => f.IdentityUserId == userId);

                if (faculty == null) return View(new List<CourseEnrolment>());

                var courseIds = faculty.CourseAssignments.Select(ca => ca.CourseId);
                var enrolments = await _context.CourseEnrolments
                    .Include(e => e.StudentProfile)
                    .Include(e => e.Course)
                    .Where(e => courseIds.Contains(e.CourseId))
                    .ToListAsync();
                return View(enrolments);
            }

            // Student — only their own enrolments
            var studentUserId = _userManager.GetUserId(User);
            var student = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.IdentityUserId == studentUserId);

            if (student == null) return View(new List<CourseEnrolment>());

            var myEnrolments = await _context.CourseEnrolments
                .Include(e => e.Course)
                .Include(e => e.StudentProfile)
                .Where(e => e.StudentProfileId == student.Id)
                .ToListAsync();
            return View(myEnrolments);
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            ViewData["StudentProfileId"] = new SelectList(
                _context.StudentProfiles
                    .Select(s => new { s.Id, Display = s.Name + " (" + s.StudentNumber + ")" }),
                "Id", "Display");

            ViewData["CourseId"] = new SelectList(
                _context.Courses
                    .Include(c => c.Branch)
                    .Select(c => new { c.Id, Display = c.Name + " — " + c.Branch.Name }),
                "Id", "Display");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create([Bind("Id,StudentProfileId,CourseId,EnrolDate,Status")] CourseEnrolment enrolment)
        {
            ModelState.Remove("StudentProfile");
            ModelState.Remove("Course");
            ModelState.Remove("AttendanceRecords");

            // ✅ Business rule: check if student is already enrolled
            var existing = await _context.CourseEnrolments
                .AnyAsync(e => e.StudentProfileId == enrolment.StudentProfileId
                            && e.CourseId == enrolment.CourseId);

            if (existing)
            {
                ModelState.AddModelError("", "This student is already enrolled in this course.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(enrolment);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Student {StudentId} enrolled in Course {CourseId}",
                    enrolment.StudentProfileId, enrolment.CourseId);

                return RedirectToAction(nameof(Index));
            }

            ViewData["StudentProfileId"] = new SelectList(
                _context.StudentProfiles
                    .Select(s => new { s.Id, Display = s.Name + " (" + s.StudentNumber + ")" }),
                "Id", "Display", enrolment.StudentProfileId);

            ViewData["CourseId"] = new SelectList(
                _context.Courses
                    .Include(c => c.Branch)
                    .Select(c => new { c.Id, Display = c.Name + " — " + c.Branch.Name }),
                "Id", "Display", enrolment.CourseId);

            return View(enrolment);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var enrolment = await _context.CourseEnrolments
                .Include(e => e.StudentProfile)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrolment == null) return NotFound();

            ViewBag.StudentDisplay = enrolment.StudentProfile?.Name
                + " (" + enrolment.StudentProfile?.StudentNumber + ")";
            ViewBag.CourseDisplay = enrolment.Course?.Name;

            return View(enrolment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentProfileId,CourseId,EnrolDate,Status")] CourseEnrolment enrolment)
        {
            if (id != enrolment.Id) return NotFound();

            ModelState.Remove("StudentProfile");
            ModelState.Remove("Course");
            ModelState.Remove("AttendanceRecords");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(enrolment);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Enrolment {Id} updated to status {Status}",
                        enrolment.Id, enrolment.Status);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.CourseEnrolments.Any(e => e.Id == enrolment.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var existing = await _context.CourseEnrolments
                .Include(e => e.StudentProfile)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            ViewBag.StudentDisplay = existing?.StudentProfile?.Name
                + " (" + existing?.StudentProfile?.StudentNumber + ")";
            ViewBag.CourseDisplay = existing?.Course?.Name;

            return View(enrolment);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var enrolment = await _context.CourseEnrolments
                .Include(e => e.StudentProfile)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrolment == null) return NotFound();
            return View(enrolment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var enrolment = await _context.CourseEnrolments.FindAsync(id);
            if (enrolment != null)
            {
                _context.CourseEnrolments.Remove(enrolment);
                _logger.LogInformation("Enrolment {Id} deleted", id);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}