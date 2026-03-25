using System.Security.Claims;
using AdoKt20RazorSignalrJwt.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AdoKt20RazorSignalrJwt.Hubs;

[Authorize]
public class ChatHub : Hub
{
	private readonly AppDbContext _dbContext;

	public ChatHub(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task SendMessage(string messageText)
	{
		var text = messageText.Trim();
		if (string.IsNullOrWhiteSpace(text) || text.Length > 400)
		{
			return;
		}

		var userIdValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(userIdValue, out var userId))
		{
			throw new HubException("user not found");
		}

		var user = await _dbContext.Users
			.FirstOrDefaultAsync(entity => entity.Id == userId);
		if (user is null)
		{
			throw new HubException("user not found");
		}

		var message = new ChatMessageEntity
		{
			Text = text,
			CreatedAtUtc = DateTime.UtcNow,
			FkUserId = user.Id,
		};

		_dbContext.ChatMessages.Add(message);
		await _dbContext.SaveChangesAsync();

		await Clients.All.SendAsync("ReceiveMessage", new
		{
			user = user.Username,
			text = message.Text,
			createdAtUtc = message.CreatedAtUtc,
		});
	}
}
