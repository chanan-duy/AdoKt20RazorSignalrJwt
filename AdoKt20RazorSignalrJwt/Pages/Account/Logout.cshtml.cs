using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdoKt20RazorSignalrJwt.Pages.Account;

public class LogoutModel : PageModel
{
	public IActionResult OnGet()
	{
		return Page();
	}

	public async Task<IActionResult> OnPostAsync()
	{
		await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
		return RedirectToPage("/Index");
	}
}
