using FixItNepal.Data;
using FixItNepal.Models;
using FixItNepal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixItNepal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Service
        public async Task<IActionResult> Index()
        {
            var services = await _context.ServiceItems.Include(s => s.ServiceCategory).ToListAsync();
            return View(services);
        }

        // GET: Service/Create
        public IActionResult Create()
        {
            var model = new ServiceItemViewModel
            {
                Categories = _context.ServiceCategories
                    .Where(c => c.IsActive)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToList()
            };
            return View(model);
        }

        // POST: Service/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceItemViewModel model)
        {
            if (ModelState.IsValid)
            {
                string imagePath = null;
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/services");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                    imagePath = uniqueFileName;
                }

                var serviceItem = new ServiceItem
                {
                    Name = model.Name,
                    Description = model.Description,
                    BasePrice = model.BasePrice,
                    ServiceCategoryId = model.ServiceCategoryId,
                    ImageUrl = imagePath,
                    IsActive = model.IsActive
                };

                _context.Add(serviceItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Repopulate categories if model state is invalid
            model.Categories = _context.ServiceCategories
                .Where(c => c.IsActive)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                     Value = c.Id.ToString(),
                     Text = c.Name
                }).ToList();
            return View(model);
        }

        // GET: Service/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var serviceItem = await _context.ServiceItems.FindAsync(id);
            if (serviceItem == null) return NotFound();

            var model = new ServiceItemViewModel
            {
                Id = serviceItem.Id,
                Name = serviceItem.Name,
                Description = serviceItem.Description,
                BasePrice = serviceItem.BasePrice,
                ServiceCategoryId = serviceItem.ServiceCategoryId,
                CurrentImageUrl = serviceItem.ImageUrl,
                IsActive = serviceItem.IsActive,
                Categories = _context.ServiceCategories
                    .Where(c => c.IsActive)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToList()
            };

            return View(model);
        }

        // POST: Service/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceItemViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var serviceItem = await _context.ServiceItems.FindAsync(id);
                    if (serviceItem == null) return NotFound();

                    serviceItem.Name = model.Name;
                    serviceItem.Description = model.Description;
                    serviceItem.BasePrice = model.BasePrice;
                    serviceItem.ServiceCategoryId = model.ServiceCategoryId;
                    serviceItem.IsActive = model.IsActive;

                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                         var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/services");
                        Directory.CreateDirectory(uploadsFolder);
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(fileStream);
                        }
                        serviceItem.ImageUrl = uniqueFileName; // Update new image
                    }

                    _context.Update(serviceItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceItemExists(model.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            model.Categories = _context.ServiceCategories
                    .Where(c => c.IsActive)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToList();
            return View(model);
        }

        // GET: Service/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var serviceItem = await _context.ServiceItems.FirstOrDefaultAsync(m => m.Id == id);
            if (serviceItem == null) return NotFound();
            return View(serviceItem);
        }

        // POST: Service/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var serviceItem = await _context.ServiceItems.FindAsync(id);
            if (serviceItem != null)
            {
                _context.ServiceItems.Remove(serviceItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceItemExists(int id)
        {
            return _context.ServiceItems.Any(e => e.Id == id);
        }
    }
}
