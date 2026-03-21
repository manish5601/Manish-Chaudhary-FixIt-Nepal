using FixItNepal.Data;
using FixItNepal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FixItNepal.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? userId, int? bookingId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.TargetUserId = userId;
            ViewBag.BookingId = bookingId;

            ApplicationUser? targetUser = null;
            if (!string.IsNullOrEmpty(userId))
            {
                targetUser = await _userManager.FindByIdAsync(userId);
            }

            return View(targetUser);
        }
    }
}
