namespace AgendaBot.Domain;

public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value) => _value = value;
    private Result(Error error) => _error = error;

    public bool IsSuccess => _error is null;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Pas de valeur : le résultat est en échec ({_error!.Code}).");

    public Error Error => _error
        ?? throw new InvalidOperationException("Pas d'erreur : le résultat est un succès.");

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);
}
