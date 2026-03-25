namespace AdoKt20RazorSignalrJwt.Data;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
// ReSharper disable PropertyCanBeMadeInitOnly.Global
public class ChatMessageEntity
{
	public int Id { get; set; }
	public string Text { get; set; } = string.Empty;
	public DateTime CreatedAtUtc { get; set; }
	public int FkUserId { get; set; }
	public UserEntity User { get; set; } = null!;
}
