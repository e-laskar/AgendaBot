namespace AgendaBot.Domain;

// Identifiants Discord (snowflakes) typés : impossible de passer un ChannelId là où un GuildId est attendu.

public sealed record GuildId(ulong Value);

public sealed record ChannelId(ulong Value);

public sealed record UserId(ulong Value);
