namespace AdoKt20RazorSignalrJwt.Contracts;

public sealed record LoginResponse(
	string Token,
	string Username,
	DateTime ExpiresAtUtc
);
