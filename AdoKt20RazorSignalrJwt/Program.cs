using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AdoKt20RazorSignalrJwt.Auth;
using AdoKt20RazorSignalrJwt.Contracts;
using AdoKt20RazorSignalrJwt.Data;
using AdoKt20RazorSignalrJwt.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AdoKt20RazorSignalrJwt;

public abstract class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
		                 ?? throw new InvalidOperationException("Missing Jwt configuration.");
		ValidateJwtOptions(jwtOptions);

		builder.Services.AddDbContext<AppDbContext>(options =>
		{
			options.UseSqlite(builder.Configuration.GetConnectionString("AppDbContext")
			                  ?? "Data Source=AppDbContext.db");
		});
		builder.Services.AddSingleton(jwtOptions);

		builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = CreateTokenValidationParameters(jwtOptions);
				options.Events = new JwtBearerEvents
				{
					OnMessageReceived = context =>
					{
						var accessToken = context.Request.Query["access_token"];
						var requestPath = context.HttpContext.Request.Path;

						if (!string.IsNullOrWhiteSpace(accessToken) && requestPath.StartsWithSegments("/chatHub"))
						{
							context.Token = accessToken;
						}

						return Task.CompletedTask;
					},
				};
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
		app.MapPost("/api/auth/login", LoginAsync);
		app.MapGet("/api/chat/messages", GetMessagesAsync)
			.RequireAuthorization();
		app.MapHub<ChatHub>("/chatHub")
			.RequireAuthorization();

		await app.RunAsync();
	}

	private static void ValidateJwtOptions(JwtOptions jwtOptions)
	{
		if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
		    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
		    string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
		{
			throw new InvalidOperationException("Jwt settings must include issuer, audience, and signing key.");
		}

		if (jwtOptions.SigningKey.Length < 32)
		{
			throw new InvalidOperationException("Jwt signing key must be at least 32 characters.");
		}
	}

	private static TokenValidationParameters CreateTokenValidationParameters(JwtOptions jwtOptions)
	{
		return new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = jwtOptions.Issuer,
			ValidateAudience = true,
			ValidAudience = jwtOptions.Audience,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
			ValidateLifetime = true,
			ClockSkew = TimeSpan.FromSeconds(30),
			NameClaimType = ClaimTypes.Name,
		};
	}

	private static async Task<IResult> LoginAsync(
		LoginRequest request,
		AppDbContext dbContext,
		IPasswordHasher<UserEntity> passwordHasher,
		JwtOptions jwtOptions
	)
	{
		var username = request.Username.Trim();
		var password = request.Password;
		var errors = new Dictionary<string, string[]>();

		if (username.Length is < 3 or > 32)
		{
			errors["username"] = ["username must be 3 to 32 characters"];
		}

		if (password.Length is < 4 or > 64)
		{
			errors["password"] = ["password must be 4 to 64 characters"];
		}

		if (errors.Count > 0)
		{
			return Results.ValidationProblem(errors);
		}

		var user = await dbContext.Users
			.FirstOrDefaultAsync(entity => entity.UsernameNormalized == username.ToUpperInvariant());
		if (user is null)
		{
			return Results.Unauthorized();
		}

		var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
		if (verifyResult == PasswordVerificationResult.Failed)
		{
			return Results.Unauthorized();
		}

		var expiresAtUtc = DateTime.UtcNow.AddMinutes(jwtOptions.ExpiresMinutes);
		var token = CreateToken(user, jwtOptions, expiresAtUtc);

		return Results.Ok(new LoginResponse(token, user.Username, expiresAtUtc));
	}

	private static async Task<IResult> GetMessagesAsync(AppDbContext dbContext)
	{
		var messages = await dbContext.ChatMessages
			.AsNoTracking()
			.Include(message => message.User)
			.OrderByDescending(message => message.CreatedAtUtc)
			.Take(50)
			.Select(message => new ChatMessageDto(
				message.User.Username,
				message.Text,
				message.CreatedAtUtc))
			.ToListAsync();

		messages.Reverse();
		return Results.Ok(messages);
	}

	private static string CreateToken(UserEntity user, JwtOptions jwtOptions, DateTime expiresAtUtc)
	{
		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.Name, user.Username),
			new(JwtRegisteredClaimNames.UniqueName, user.Username),
			new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
		};

		var signingCredentials = new SigningCredentials(
			new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
			SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			jwtOptions.Issuer,
			jwtOptions.Audience,
			claims,
			DateTime.UtcNow,
			expiresAtUtc,
			signingCredentials);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
