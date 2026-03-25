namespace AdoKt20RazorSignalrJwt.Contracts;

public sealed record ChatMessageDto(
	string Username,
	string Text,
	DateTime CreatedAtUtc
);
