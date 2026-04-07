using Entities.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using oop_s2_1_mvc_79189.Data;

namespace oop_s2_1_mvc_79189.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class CoursesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(AppDbContext context, ILogger<CoursesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.Branch)
                .Include(c => c.Enrolments)
                .Include(c => c.FacultyAssignments)
                    .ThenInclude(fa => fa.FacultyProfile)
                .ToListAsync();
            return View(courses);
        }

        // ✅ Updated — loads full relationship tree
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Branch)
                .Include(c => c.FacultyAssignments)
                    .ThenInclude(fa => fa.FacultyProfile)
                .Include(c => c.Enrolments)
                    .ThenInclude(e => e.StudentProfile)
                .Include(c => c.Assignments)
                    .ThenInclude(a => a.Results)
                .Include(c => c.Exams)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();
            return View(course);
        }

        public IActionResult Create()
        {
            ViewData["BranchId"] = new SelectList(
                _context.Branches.Select(b => new { b.Id, Display = b.Name + " — " + b.Address }),
                "Id", "Display");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,BranchId,StartDate,EndDate")] Course course)
        {
            ModelState.Remove("Branch");
            ModelState.Remove("Enrolments");
            ModelState.Remove("Assignments");
            ModelState.Remove("Exams");
            ModelState.Remove("FacultyAssignments");

            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Course created: {Name}", course.Name);
                return RedirectToAction(nameof(Index));
            }

            ViewData["BranchId"] = new SelectList(
                _context.Branches.Select(b => new { b.Id, Display = b.Name + " — " + b.Address }),
                "Id", "Display", course.BranchId);
            return View(course);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            ViewData["BranchId"] = new SelectList(
                _context.Branches.Select(b => new { b.Id, Display = b.Name + " — " + b.Address }),
                "Id", "Display", course.BranchId);
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,BranchId,StartDate,EndDate")] Course course)
        {
            if (id != course.Id) return NotFound();

            ModelState.Remove("Branch");
            ModelState.Remove("Enrolments");
            ModelState.Remove("Assignments");
            ModelState.Remove("Exams");
            ModelState.Remove("FacultyAssignments");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Course updated: {Name}", course.Name);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Courses.Any(c => c.Id == course.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["BranchId"] = new SelectList(
                _context.Branches.Select(b => new { b.Id, Display = b.Name + " — " + b.Address }),
                "Id", "Display", course.BranchId);
            return View(course);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Branch)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();
            return View(course);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
                _logger.LogInformation("Course deleted: {Id}", id);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}