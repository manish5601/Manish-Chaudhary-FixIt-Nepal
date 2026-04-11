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
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized"));

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

            return Ok(ApiResponse<object>.SuccessResponse(history, "Chat history retrieved successfully"));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized"));

            var count = await _context.ChatMessages
                .CountAsync(m => m.ReceiverId == currentUserId && !m.IsRead);

            return Ok(ApiResponse<object>.SuccessResponse(new { count }, "Unread message count retrieved successfully"));
        }

        [HttpPost("mark-read/{otherUserId}")]
        public async Task<IActionResult> MarkAsRead(string otherUserId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized"));

            var messages = await _context.ChatMessages
                .Where(m => m.ReceiverId == currentUserId && m.SenderId == otherUserId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in messages)
            {
                msg.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResponse(null, "Messages marked as read"));
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized"));

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

            return Ok(ApiResponse<object>.SuccessResponse(conversations, "Conversations retrieved successfully"));
        }
    }
}
