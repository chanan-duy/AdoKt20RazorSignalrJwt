using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdoKt20RazorSignalrJwt.Data;

public static class AppDbSeeder
{
	public static async Task SeedAsync(AppDbContext dbContext, IPasswordHasher<UserEntity> passwordHasher)
	{
		var users = new[]
		{
			CreateUser("user1", "pwd1", passwordHasher),
			CreateUser("user2", "pwd2", passwordHasher),
			CreateUser("user3", "pwd3", passwordHasher),
		};

		foreach (var user in users)
		{
			var exists = await dbContext.Users
				.AnyAsync(entity => entity.UsernameNormalized == user.UsernameNormalized);
			if (exists)
			{
				continue;
			}

			dbContext.Users.Add(user);
		}

		await dbContext.SaveChangesAsync();
	}

	private static UserEntity CreateUser(string username, string password, IPasswordHasher<UserEntity> passwordHasher)
	{
		var user = new UserEntity
		{
			Username = username,
			UsernameNormalized = username.ToUpperInvariant(),
			RegisteredAtUtc = DateTime.UtcNow,
		};

		user.PasswordHash = passwordHasher.HashPassword(user, password);
		return user;
	}
}
