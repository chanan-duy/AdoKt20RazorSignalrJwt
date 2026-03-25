using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AdoKt20RazorSignalrJwt.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AdoKt20RazorSignalrJwt.Pages;

public class IndexModel : PageModel
{
	private readonly AppDbContext _dbContext;
	private readonly IPasswordHasher<UserEntity> _passwordHasher;

	public IndexModel(AppDbContext dbContext, IPasswordHasher<UserEntity> passwordHasher)
	{
		_dbContext = dbContext;
		_passwordHasher = passwordHasher;
	}

	[BindProperty]
	public LoginInputModel LoginInput { get; set; } = new();

	public string CurrentUsername { get; private set; } = string.Empty;
	public List<ChatMessageViewModel> Messages { get; private set; } = [];
	public string LoginErrorMessage { get; private set; } = string.Empty;

	public async Task OnGetAsync()
	{
		await LoadChatAsync();
	}

	public async Task<IActionResult> OnPostLoginAsync()
	{
		if (User.Identity?.IsAuthenticated == true)
		{
			return RedirectToPage();
		}

		if (!TryValidateModel(LoginInput, nameof(LoginInput)))
		{
			return Page();
		}

		var usernameNormalized = LoginInput.Username.Trim().ToUpperInvariant();
		var user = await _dbContext.Users
			.FirstOrDefaultAsync(entity => entity.UsernameNormalized == usernameNormalized);
		if (user is null)
		{
			LoginErrorMessage = "invalid user or password";
			return Page();
		}

		var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, LoginInput.Password);
		if (verifyResult == PasswordVerificationResult.Failed)
		{
			LoginErrorMessage = "invalid user or password";
			return Page();
		}

		await SignInAsync(user);
		return RedirectToPage();
	}

	private async Task LoadChatAsync()
	{
		if (User.Identity?.IsAuthenticated != true)
		{
			return;
		}

		CurrentUsername = User.Identity.Name ?? "unknown";

		Messages = await _dbContext.ChatMessages
			.AsNoTracking()
			.Include(message => message.User)
			.OrderByDescending(message => message.CreatedAtUtc)
			.Take(50)
			.Select(message => new ChatMessageViewModel
			{
				Username = message.User.Username,
				Text = message.Text,
				CreatedAtLocal = message.CreatedAtUtc.ToLocalTime(),
			})
			.ToListAsync();

		Messages.Reverse();
	}

	private async Task SignInAsync(UserEntity user)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.Name, user.Username),
		};

		var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
		var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

		await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
	}

	public class LoginInputModel
	{
		[Required]
		[StringLength(32, MinimumLength = 3)]
		[Display(Name = "user")]
		public string Username { get; set; } = string.Empty;

		[Required]
		[StringLength(64, MinimumLength = 4)]
		[DataType(DataType.Password)]
		[Display(Name = "password")]
		public string Password { get; set; } = string.Empty;
	}

	public class ChatMessageViewModel
	{
		public string Username { get; set; } = string.Empty;
		public string Text { get; set; } = string.Empty;
		public DateTime CreatedAtLocal { get; set; }
	}
}
