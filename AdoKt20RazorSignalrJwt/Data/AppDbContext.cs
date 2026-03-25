using Microsoft.EntityFrameworkCore;

namespace AdoKt20RazorSignalrJwt.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}

	public DbSet<UserEntity> Users { get; set; }
	public DbSet<ChatMessageEntity> ChatMessages { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<UserEntity>()
			.HasIndex(user => user.UsernameNormalized)
			.IsUnique();

		modelBuilder.Entity<UserEntity>()
			.Property(user => user.Username)
			.HasMaxLength(32);

		modelBuilder.Entity<UserEntity>()
			.Property(user => user.UsernameNormalized)
			.HasMaxLength(32);

		modelBuilder.Entity<ChatMessageEntity>()
			.Property(message => message.Text)
			.HasMaxLength(400);

		modelBuilder.Entity<ChatMessageEntity>()
			.HasOne(message => message.User)
			.WithMany(user => user.Messages)
			.HasForeignKey(message => message.FkUserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
