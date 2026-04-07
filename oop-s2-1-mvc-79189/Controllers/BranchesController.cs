using Entities.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s2_1_mvc_79189.Data;

namespace oop_s2_1_mvc_79189.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class BranchesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BranchesController> _logger;

        public BranchesController(AppDbContext context, ILogger<BranchesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var branches = await _context.Branches
                .Include(b => b.Courses)
                .ToListAsync();
            return View(branches);
        }

        // ✅ Updated — loads full relationship tree
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var branch = await _context.Branches
                .Include(b => b.Courses)
                    .ThenInclude(c => c.Enrolments)
                        .ThenInclude(e => e.StudentProfile)
                .Include(b => b.Courses)
                    .ThenInclude(c => c.FacultyAssignments)
                        .ThenInclude(fa => fa.FacultyProfile)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null) return NotFound();
            return View(branch);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Address")] Branch branch)
        {
            if (ModelState.IsValid)
            {
                _context.Add(branch);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Branch created: {Name}", branch.Name);
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound();
            return View(branch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Address")] Branch branch)
        {
            if (id != branch.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(branch);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Branch updated: {Name}", branch.Name);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Branches.Any(b => b.Id == branch.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var branch = await _context.Branches
                .Include(b => b.Courses)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null) return NotFound();
            return View(branch);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch != null)
            {
                _context.Branches.Remove(branch);
                _logger.LogInformation("Branch deleted: {Id}", id);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}