namespace AdoKt20RazorSignalrJwt.Contracts;

public sealed record LoginRequest(
	string Username,
	string Password
);
