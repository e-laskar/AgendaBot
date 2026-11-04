namespace AgendaBot.Configuration;

/// <summary>
/// Chargeur minimaliste de fichier <c>.env</c> (format <c>CLE=valeur</c>).
/// Les variables déjà présentes dans l'environnement ne sont jamais écrasées :
/// l'environnement réel (shell, Docker, systemd…) reste prioritaire sur le fichier.
/// </summary>
public static class DotEnv
{
    /// <summary>
    /// Charge le premier fichier trouvé parmi <paramref name="path"/> (si fourni),
    /// <c>.env</c> dans le répertoire courant ou l'un de ses parents (ex. racine du dépôt
    /// quand l'IDE lance depuis <c>src/AgendaBot.Host</c>), puis <c>.env</c> à côté de l'exécutable.
    /// Ne fait rien si aucun n'existe.
    /// </summary>
    /// <returns>Le chemin du fichier chargé, ou <c>null</c>.</returns>
    public static string? Load(string? path = null)
    {
        IEnumerable<string> candidates = path is not null
            ? [path]
            : SelfAndParents(Directory.GetCurrentDirectory())
                .Select(dir => Path.Combine(dir, ".env"))
                .Append(Path.Combine(AppContext.BaseDirectory, ".env"));

        var file = candidates.FirstOrDefault(File.Exists);
        if (file is null)
            return null;

        foreach (var (key, value) in Parse(File.ReadAllLines(file)))
        {
            // Une valeur vide est ignorée : sinon "Discord__Token=" écraserait le token des user-secrets.
            if (value.Length > 0 && Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }

        return file;
    }

    private static IEnumerable<string> SelfAndParents(string directory)
    {
        for (var dir = new DirectoryInfo(directory); dir is not null; dir = dir.Parent)
            yield return dir.FullName;
    }

    internal static IEnumerable<(string Key, string Value)> Parse(IEnumerable<string> lines)
    {
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            if (line.StartsWith("export ", StringComparison.Ordinal))
                line = line["export ".Length..].TrimStart();

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            if (value.Length > 0 && (value[0] == '"' || value[0] == '\''))
            {
                var quote = value[0];
                var end = value.IndexOf(quote, 1);
                if (end > 0)
                {
                    // Tout ce qui suit le guillemet fermant (ex. commentaire) est ignoré.
                    value = value[1..end];
                    if (quote == '"')
                        value = value.Replace("\\n", "\n");
                }
            }
            else
            {
                // Commentaire en fin de ligne pour les valeurs non entre guillemets : "CLE=valeur # commentaire"
                var comment = value.IndexOf(" #", StringComparison.Ordinal);
                if (comment >= 0)
                    value = value[..comment].TrimEnd();
            }

            yield return (key, value);
        }
    }
}
