using FixItNepal.Data;
using FixItNepal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServiceCategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceCategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ServiceCategory
        public async Task<IActionResult> Index()
        {
            return View(await _context.ServiceCategories.ToListAsync());
        }

        // GET: ServiceCategory/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ServiceCategory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,IconPath,IsActive")] ServiceCategory serviceCategory)
        {
            // Set default icon if not provided
            if (string.IsNullOrEmpty(serviceCategory.IconPath))
            {
                serviceCategory.IconPath = "bi-tools";
                // Clear validation error for IconPath if it exists (since it's now set)
                if (ModelState.ContainsKey("IconPath"))
                {
                    ModelState.Remove("IconPath");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(serviceCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(serviceCategory);
        }

        // GET: ServiceCategory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var serviceCategory = await _context.ServiceCategories.FindAsync(id);
            if (serviceCategory == null) return NotFound();
            return View(serviceCategory);
        }

        // POST: ServiceCategory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,IconPath,IsActive")] ServiceCategory serviceCategory)
        {
            if (id != serviceCategory.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(serviceCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceCategoryExists(serviceCategory.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(serviceCategory);
        }

        // GET: ServiceCategory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var serviceCategory = await _context.ServiceCategories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (serviceCategory == null) return NotFound();

            return View(serviceCategory);
        }

        // POST: ServiceCategory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var serviceCategory = await _context.ServiceCategories.FindAsync(id);
            if (serviceCategory != null)
            {
                _context.ServiceCategories.Remove(serviceCategory);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceCategoryExists(int id)
        {
            return _context.ServiceCategories.Any(e => e.Id == id);
        }
    }
}
