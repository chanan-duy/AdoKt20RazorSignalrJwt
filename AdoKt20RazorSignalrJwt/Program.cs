using AdoKt20RazorSignalrJwt.Data;
using AdoKt20RazorSignalrJwt.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdoKt20RazorSignalrJwt;

public abstract class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddDbContext<AppDbContext>(options =>
		{
			options.UseSqlite(builder.Configuration.GetConnectionString("AppDbContext")
			                  ?? "Data Source=AppDbContext.db");
		});

		builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
			.AddCookie(options =>
			{
				options.LoginPath = "/";
				options.LogoutPath = "/Account/Logout";
				options.AccessDeniedPath = "/";
			});
		builder.Services.AddAuthorization();

		builder.Services.AddScoped<IPasswordHasher<UserEntity>, PasswordHasher<UserEntity>>();
		builder.Services.AddRazorPages();
		builder.Services.AddSignalR();

		var app = builder.Build();

		using (var scope = app.Services.CreateScope())
		{
			var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserEntity>>();
			await dbContext.Database.EnsureCreatedAsync();
			await AppDbSeeder.SeedAsync(dbContext, passwordHasher);
		}

		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Error");
			app.UseHsts();
		}

		app.UseHttpsRedirection();

		app.UseRouting();

		app.UseAuthentication();
		app.UseAuthorization();

		app.MapStaticAssets();
		app.MapRazorPages()
			.WithStaticAssets();
		app.MapHub<ChatHub>("/chatHub")
			.RequireAuthorization();

		await app.RunAsync();
	}
}
