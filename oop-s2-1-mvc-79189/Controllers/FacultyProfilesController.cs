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
    public class FacultyProfilesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<FacultyProfilesController> _logger;

        public FacultyProfilesController(AppDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<FacultyProfilesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Admin sees all faculty, Faculty sees only themselves
        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Administrator"))
            {
                var all = await _context.FacultyProfiles
                    .Include(f => f.CourseAssignments)
                        .ThenInclude(ca => ca.Course)
                    .ToListAsync();
                return View(all);
            }

            // Faculty — find their own profile
            var userId = _userManager.GetUserId(User);
            var mine = await _context.FacultyProfiles
                .Include(f => f.CourseAssignments)
                    .ThenInclude(ca => ca.Course)
                .Where(f => f.IdentityUserId == userId)
                .ToListAsync();
            return View(mine);
        }

        [Authorize(Roles = "Administrator,Faculty")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var profile = await _context.FacultyProfiles
                .Include(f => f.CourseAssignments)
                    .ThenInclude(ca => ca.Course)
                        .ThenInclude(c => c.Enrolments)
                            .ThenInclude(e => e.StudentProfile)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (profile == null) return NotFound();

            // ✅ Faculty can only view their own profile
            if (User.IsInRole("Faculty"))
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
        public async Task<IActionResult> Create([Bind("Id,IdentityUserId,Name,Email,Phone")] FacultyProfile profile)
        {
            ModelState.Remove("CourseAssignments");

            if (ModelState.IsValid)
            {
                _context.Add(profile);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Faculty profile created: {Name}", profile.Name);
                return RedirectToAction(nameof(Index));
            }
            return View(profile);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var profile = await _context.FacultyProfiles.FindAsync(id);
            if (profile == null) return NotFound();
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,IdentityUserId,Name,Email,Phone")] FacultyProfile profile)
        {
            if (id != profile.Id) return NotFound();

            ModelState.Remove("CourseAssignments");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(profile);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Faculty profile updated: {Name}", profile.Name);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.FacultyProfiles.Any(f => f.Id == profile.Id))
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

            var profile = await _context.FacultyProfiles
                .FirstOrDefaultAsync(f => f.Id == id);

            if (profile == null) return NotFound();
            return View(profile);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var profile = await _context.FacultyProfiles.FindAsync(id);
            if (profile != null)
            {
                _context.FacultyProfiles.Remove(profile);
                _logger.LogInformation("Faculty profile deleted: {Id}", id);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}