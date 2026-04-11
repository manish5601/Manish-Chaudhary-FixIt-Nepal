using FixItNepal.Data;
using FixItNepal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FixItNepal.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceCategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ServiceCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ServiceCategories
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetServiceCategories()
        {
            var categories = await _context.ServiceCategories.ToListAsync();
            return Ok(ApiResponse<object>.SuccessResponse(categories, "Service categories retrieved successfully"));
        }

        // GET: api/ServiceCategories/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetServiceCategory(int id)
        {
            var serviceCategory = await _context.ServiceCategories.FindAsync(id);

            if (serviceCategory == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Service category not found"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(serviceCategory, "Service category retrieved successfully"));
        }

        // POST: api/ServiceCategories
        [HttpPost]
        [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")] // Restrict creation to Admins and use JWT
        public async Task<IActionResult> PostServiceCategory(ServiceCategory serviceCategory)
        {
            // Remove Id from ModelState if it's there, as it's auto-generated
            ModelState.Remove(nameof(ServiceCategory.Id));

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))));
            }

            _context.ServiceCategories.Add(serviceCategory);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(serviceCategory, "Service category created successfully"));
        }

        // PUT: api/ServiceCategories/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")] // Restrict updates to Admins and use JWT
        public async Task<IActionResult> PutServiceCategory(int id, ServiceCategory serviceCategory)
        {
            if (id != serviceCategory.Id)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("ID mismatch"));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))));
            }

            _context.Entry(serviceCategory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ServiceCategoryExists(id))
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Service category not found"));
                }
                else
                {
                    throw;
                }
            }

            return Ok(ApiResponse<object>.SuccessResponse(serviceCategory, "Service category updated successfully"));
        }

        // DELETE: api/ServiceCategories/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")] // Restrict deletion to Admins and use JWT
        public async Task<IActionResult> DeleteServiceCategory(int id)
        {
            var serviceCategory = await _context.ServiceCategories.FindAsync(id);
            if (serviceCategory == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Service category not found"));
            }

            _context.ServiceCategories.Remove(serviceCategory);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(null, "Service Category deleted successfully"));
        }

        private bool ServiceCategoryExists(int id)
        {
            return _context.ServiceCategories.Any(e => e.Id == id);
        }
    }
}
