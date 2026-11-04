namespace AgendaBot.Configuration;

public static class DotEnv
{
    private const string FileName = ".env";

    public static string? Load(string? path = null)
    {
        IEnumerable<string> candidates = path is not null ? [path] : DefaultLocations();
        var file = candidates.FirstOrDefault(File.Exists);
        if (file is null)
            return null;

        foreach (var (key, value) in Parse(File.ReadAllLines(file)).Where(HasValue))
            SetUnlessAlreadyDefinedInEnvironment(key, value);

        return file;
    }

    internal static IEnumerable<(string Key, string Value)> Parse(IEnumerable<string> lines)
    {
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (IsBlankOrComment(line))
                continue;

            line = WithoutExportPrefix(line);
            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            yield return (line[..separator].Trim(), ParseValue(line[(separator + 1)..].Trim()));
        }
    }

    private static IEnumerable<string> DefaultLocations() =>
        CurrentDirectoryAndItsParents()
            .Select(directory => Path.Combine(directory, FileName))
            .Append(Path.Combine(AppContext.BaseDirectory, FileName));

    private static IEnumerable<string> CurrentDirectoryAndItsParents()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
            yield return directory.FullName;
    }

    private static bool HasValue((string Key, string Value) entry) => entry.Value.Length > 0;

    private static void SetUnlessAlreadyDefinedInEnvironment(string key, string value)
    {
        if (Environment.GetEnvironmentVariable(key) is null)
            Environment.SetEnvironmentVariable(key, value);
    }

    private static bool IsBlankOrComment(string line) => line.Length == 0 || line.StartsWith('#');

    private static string WithoutExportPrefix(string line) =>
        line.StartsWith("export ", StringComparison.Ordinal) ? line["export ".Length..].TrimStart() : line;

    private static string ParseValue(string value) =>
        StartsWithQuote(value) ? ContentBetweenQuotes(value) : WithoutTrailingComment(value);

    private static bool StartsWithQuote(string value) => value.Length > 0 && value[0] is '"' or '\'';

    private static string ContentBetweenQuotes(string value)
    {
        var quote = value[0];
        var closingQuote = value.IndexOf(quote, 1);
        if (closingQuote < 0)
            return value;

        var content = value[1..closingQuote];
        return quote == '"' ? WithEscapedNewLinesExpanded(content) : content;
    }

    private static string WithEscapedNewLinesExpanded(string value) => value.Replace("\\n", "\n");

    private static string WithoutTrailingComment(string value)
    {
        var commentStart = value.IndexOf(" #", StringComparison.Ordinal);
        return commentStart >= 0 ? value[..commentStart].TrimEnd() : value;
    }
}
