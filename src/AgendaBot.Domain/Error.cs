namespace AgendaBot.Domain;

/// <summary>Erreur métier attendue : un code stable (pour le code) et un message pour l'utilisateur.</summary>
public sealed record Error(string Code, string Message);
