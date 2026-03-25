namespace AdoKt20RazorSignalrJwt.Data;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
// ReSharper disable PropertyCanBeMadeInitOnly.Global
public class UserEntity
{
	public int Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string UsernameNormalized { get; set; } = string.Empty;
	public string PasswordHash { get; set; } = string.Empty;
	public DateTime RegisteredAtUtc { get; set; }
	public List<ChatMessageEntity> Messages { get; set; } = [];
}
