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
    [Authorize]
    public class ChatApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChatApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("history/{otherUserId}")]
        public async Task<IActionResult> GetChatHistory(string otherUserId, [FromQuery] int? bookingId = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var query = _context.ChatMessages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == currentUserId));

            if (bookingId.HasValue)
            {
                query = query.Where(m => m.BookingId == bookingId);
            }

            var history = await query
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.ReceiverId,
                    m.Message,
                    m.Timestamp,
                    m.IsRead,
                    m.BookingId
                })
                .ToListAsync();

            return Ok(history);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var count = await _context.ChatMessages
                .CountAsync(m => m.ReceiverId == currentUserId && !m.IsRead);

            return Ok(new { count });
        }

        [HttpPost("mark-read/{otherUserId}")]
        public async Task<IActionResult> MarkAsRead(string otherUserId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var messages = await _context.ChatMessages
                .Where(m => m.ReceiverId == currentUserId && m.SenderId == otherUserId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in messages)
            {
                msg.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            // Get unique users the current user has chatted with
            var sentTo = await _context.ChatMessages
                .Where(m => m.SenderId == currentUserId)
                .Select(m => m.ReceiverId)
                .Distinct()
                .ToListAsync();

            var receivedFrom = await _context.ChatMessages
                .Where(m => m.ReceiverId == currentUserId)
                .Select(m => m.SenderId)
                .Distinct()
                .ToListAsync();

            var userIds = sentTo.Union(receivedFrom).Distinct();

            var conversations = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.ProfilePicture,
                    u.UserName,
                    LastMessage = _context.ChatMessages
                        .Where(m => (m.SenderId == currentUserId && m.ReceiverId == u.Id) ||
                                    (m.SenderId == u.Id && m.ReceiverId == currentUserId))
                        .OrderByDescending(m => m.Timestamp)
                        .Select(m => m.Message)
                        .FirstOrDefault(),
                    LastTimestamp = _context.ChatMessages
                        .Where(m => (m.SenderId == currentUserId && m.ReceiverId == u.Id) ||
                                    (m.SenderId == u.Id && m.ReceiverId == currentUserId))
                        .OrderByDescending(m => m.Timestamp)
                        .Select(m => m.Timestamp)
                        .FirstOrDefault(),
                    UnreadCount = _context.ChatMessages
                        .Count(m => m.ReceiverId == currentUserId && m.SenderId == u.Id && !m.IsRead)
                })
                .OrderByDescending(c => c.LastTimestamp)
                .ToListAsync();

            return Ok(conversations);
        }
    }
}
