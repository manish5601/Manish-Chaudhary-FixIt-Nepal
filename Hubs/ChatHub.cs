using FixItNepal.Data;
using FixItNepal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FixItNepal.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string receiverId, string message, int? bookingId)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(senderId))
                throw new HubException("User not authenticated");

            if (string.IsNullOrWhiteSpace(message))
                throw new HubException("Message cannot be empty");

            // Save to database
            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsRead = false,
                BookingId = bookingId
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Broadcast to receiver
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, chatMessage);
            
            // Send back to sender for consistency (or just acknowledge)
            await Clients.Caller.SendAsync("MessageSent", chatMessage);
        }

        public async Task MarkAsRead(int messageId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var message = await _context.ChatMessages.FindAsync(messageId);

            if (message != null && message.ReceiverId == userId)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
                await Clients.User(message.SenderId).SendAsync("MessageRead", messageId);
            }
        }
    }
}
