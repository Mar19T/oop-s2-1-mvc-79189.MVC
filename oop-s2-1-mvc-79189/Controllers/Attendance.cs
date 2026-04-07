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
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(AppDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<AttendanceController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [Authorize(Roles = "Administrator,Faculty,Student")]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Administrator"))
            {
                var all = await _context.AttendanceRecords
                    .Include(a => a.CourseEnrolment)
                        .ThenInclude(e => e.StudentProfile)
                    .Include(a => a.CourseEnrolment)
                        .ThenInclude(e => e.Course)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();
                return View(all);
            }

            if (User.IsInRole("Faculty"))
            {
                var userId = _userManager.GetUserId(User);
                var faculty = await _context.FacultyProfiles
                    .Include(f => f.CourseAssignments)
                    .FirstOrDefaultAsync(f => f.IdentityUserId == userId);

                if (faculty == null) return View(new List<AttendanceRecord>());

                var courseIds = faculty.CourseAssignments.Select(ca => ca.CourseId);
                var records = await _context.AttendanceRecords
                    .Include(a => a.CourseEnrolment)
                        .ThenInclude(e => e.StudentProfile)
                    .Include(a => a.CourseEnrolment)
                        .ThenInclude(e => e.Course)
                    .Where(a => courseIds.Contains(a.CourseEnrolment.CourseId))
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();
                return View(records);
            }

            // Student — only their own attendance
            var studentUserId = _userManager.GetUserId(User);
            var student = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.IdentityUserId == studentUserId);

            if (student == null) return View(new List<AttendanceRecord>());

            var myRecords = await _context.AttendanceRecords
                .Include(a => a.CourseEnrolment)
                    .ThenInclude(e => e.Course)
                .Include(a => a.CourseEnrolment)
                    .ThenInclude(e => e.StudentProfile)
                .Where(a => a.CourseEnrolment.StudentProfileId == student.Id)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
            return View(myRecords);
        }

        [Authorize(Roles = "Administrator,Faculty")]
        public IActionResult Create()
        {
            ViewData["CourseEnrolmentId"] = new SelectList(
                _context.CourseEnrolments
                    .Include(e => e.StudentProfile)
                    .Include(e => e.Course)
                    .Where(e => e.Status == EnrolmentStatus.Active)
                    .Select(e => new
                    {
                        e.Id,
                        Display = e.StudentProfile.Name + " — " + e.Course.Name
                    }),
                "Id", "Display");

            ViewBag.DefaultDate = DateTime.Today.ToString("yyyy-MM-dd");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> Create([Bind("Id,CourseEnrolmentId,WeekNumber,Date,Present")] AttendanceRecord record)
        {
            ModelState.Remove("CourseEnrolment");

            if (ModelState.IsValid)
            {
                _context.Add(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Attendance recorded for EnrolmentId {EnrolmentId}, Week {Week}, Present {Present}",
                    record.CourseEnrolmentId, record.WeekNumber, record.Present);

                return RedirectToAction(nameof(Index));
            }

            ViewData["CourseEnrolmentId"] = new SelectList(
                _context.CourseEnrolments
                    .Include(e => e.StudentProfile)
                    .Include(e => e.Course)
                    .Where(e => e.Status == EnrolmentStatus.Active)
                    .Select(e => new
                    {
                        e.Id,
                        Display = e.StudentProfile.Name + " — " + e.Course.Name
                    }),
                "Id", "Display", record.CourseEnrolmentId);

            return View(record);
        }

        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.AttendanceRecords
                .Include(a => a.CourseEnrolment)
                    .ThenInclude(e => e.StudentProfile)
                .Include(a => a.CourseEnrolment)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (record == null) return NotFound();

            ViewBag.EnrolmentDisplay = record.CourseEnrolment?.StudentProfile?.Name
                + " — " + record.CourseEnrolment?.Course?.Name;

            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CourseEnrolmentId,WeekNumber,Date,Present")] AttendanceRecord record)
        {
            if (id != record.Id) return NotFound();

            ModelState.Remove("CourseEnrolment");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(record);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Attendance record {Id} updated, Present {Present}",
                        record.Id, record.Present);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.AttendanceRecords.Any(a => a.Id == record.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var existing = await _context.AttendanceRecords
                .Include(a => a.CourseEnrolment)
                    .ThenInclude(e => e.StudentProfile)
                .Include(a => a.CourseEnrolment)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(a => a.Id == id);

            ViewBag.EnrolmentDisplay = existing?.CourseEnrolment?.StudentProfile?.Name
                + " — " + existing?.CourseEnrolment?.Course?.Name;

            return View(record);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.AttendanceRecords
                .Include(a => a.CourseEnrolment)
                    .ThenInclude(e => e.StudentProfile)
                .Include(a => a.CourseEnrolment)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (record == null) return NotFound();
            return View(record);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var record = await _context.AttendanceRecords.FindAsync(id);
            if (record != null)
            {
                _context.AttendanceRecords.Remove(record);
                _logger.LogInformation("Attendance record {Id} deleted", id);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}