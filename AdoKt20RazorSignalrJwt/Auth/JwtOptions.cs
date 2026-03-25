namespace AdoKt20RazorSignalrJwt.Auth;

public class JwtOptions
{
	public const string SectionName = "Jwt";

	public string Issuer { get; set; } = string.Empty;
	public string Audience { get; set; } = string.Empty;
	public string SigningKey { get; set; } = string.Empty;
	public int ExpiresMinutes { get; set; } = 480;
}
