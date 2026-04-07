using Entities.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s2_1_mvc_79189.Data;

namespace oop_s2_1_mvc_79189.Controllers
{
    [Authorize]
    public class StudentProfilesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<StudentProfilesController> _logger;

        public StudentProfilesController(AppDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<StudentProfilesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Admin sees all students, Student sees only themselves
        [Authorize(Roles = "Administrator,Student")]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Administrator"))
            {
                var all = await _context.StudentProfiles
                    .Include(s => s.Enrolments)
                    .ToListAsync();
                return View(all);
            }

            // Student — find their own profile
            var userId = _userManager.GetUserId(User);
            var mine = await _context.StudentProfiles
                .Include(s => s.Enrolments)
                .Where(s => s.IdentityUserId == userId)
                .ToListAsync();
            return View(mine);
        }

        [Authorize(Roles = "Administrator,Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var profile = await _context.StudentProfiles
                .Include(s => s.Enrolments)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (profile == null) return NotFound();

            // ✅ Students can only view their own profile
            if (User.IsInRole("Student"))
            {
                var userId = _userManager.GetUserId(User);
                if (profile.IdentityUserId != userId)
                    return Forbid();
            }

            return View(profile);
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create([Bind("Id,IdentityUserId,Name,Email,Phone,Address,DateOfBirth,StudentNumber")] StudentProfile profile)
        {
            ModelState.Remove("Enrolments");
            ModelState.Remove("AssignmentResults");
            ModelState.Remove("ExamResults");

            if (ModelState.IsValid)
            {
                _context.Add(profile);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Student profile created: {Name}", profile.Name);
                return RedirectToAction(nameof(Index));
            }
            return View(profile);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var profile = await _context.StudentProfiles.FindAsync(id);
            if (profile == null) return NotFound();
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,IdentityUserId,Name,Email,Phone,Address,DateOfBirth,StudentNumber")] StudentProfile profile)
        {
            if (id != profile.Id) return NotFound();

            ModelState.Remove("Enrolments");
            ModelState.Remove("AssignmentResults");
            ModelState.Remove("ExamResults");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(profile);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Student profile updated: {Name}", profile.Name);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.StudentProfiles.Any(s => s.Id == profile.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(profile);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.Id == id);

            if (profile == null) return NotFound();
            return View(profile);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var profile = await _context.StudentProfiles.FindAsync(id);
            if (profile != null)
            {
                _context.StudentProfiles.Remove(profile);
                _logger.LogInformation("Student profile deleted: {Id}", id);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}