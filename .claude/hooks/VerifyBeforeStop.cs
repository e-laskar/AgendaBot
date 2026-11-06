using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

string[] watchedPaths =
    ["src", "tests", "Directory.Build.props", "Directory.Packages.props", "global.json", "AgendaBot.slnx"];

var projectDir = Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR") is { Length: > 0 } dir
    ? dir
    : Directory.GetCurrentDirectory();
Directory.SetCurrentDirectory(projectDir);

var stateDir = Path.Combine(".artifacts", "claude-stop-hook");
Directory.CreateDirectory(stateDir);
var lastGreenFile = Path.Combine(stateDir, "last-green");
var lastRedFile = Path.Combine(stateDir, "last-red");
var logFile = Path.Combine(stateDir, "last-run.log");

var stopHookActive = ReadStopHookActive(Console.In.ReadToEnd());
var current = WorkingTreeFingerprint();

if (current == ReadOrEmpty(lastGreenFile))
    return 0;

if (stopHookActive && current == ReadOrEmpty(lastRedFile))
    return Warn("Hook Stop : build ou tests toujours rouges, rien n'a changé depuis le dernier échec. Arrêt laissé passer.");

(string Label, string Arguments)[] steps =
[
    ("build Release", "build --configuration Release"),
    ("tests unitaires", "test --no-build --configuration Release --project tests/AgendaBot.UnitTests"),
    ("tests d'architecture", "test --no-build --configuration Release --project tests/AgendaBot.ArchitectureTests"),
];

foreach (var (label, arguments) in steps)
{
    var (exitCode, output) = Run("dotnet", arguments);
    if (exitCode == 0) continue;

    File.WriteAllText(logFile, output);
    File.WriteAllText(lastRedFile, current);
    Console.Error.WriteLine(FailureReport(label, output));
    return 2;
}

File.WriteAllText(lastGreenFile, current);
File.Delete(lastRedFile);
return 0;

static bool ReadStopHookActive(string input)
{
    try
    {
        using var json = JsonDocument.Parse(input);
        return json.RootElement.TryGetProperty("stop_hook_active", out var value)
               && value.ValueKind == JsonValueKind.True;
    }
    catch (JsonException)
    {
        return false;
    }
}

string WorkingTreeFingerprint()
{
    var index = Path.GetTempFileName();
    try
    {
        var gitEnv = new Dictionary<string, string> { ["GIT_INDEX_FILE"] = index };
        Run("git", "read-tree HEAD", gitEnv);
        Run("git", $"add -A -- {string.Join(' ', watchedPaths)}", gitEnv);
        var tree = Run("git", "write-tree", gitEnv).Output.Trim();

        var hashes = watchedPaths.Select(path => Run("git", $"rev-parse --quiet --verify {tree}:{path}").Output.Trim());
        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(string.Join('\n', hashes))));
    }
    finally
    {
        File.Delete(index);
    }
}

string FailureReport(string label, string output)
{
    var errorLines = output.Split('\n')
        .Select(line => line.TrimEnd())
        .Where(line => line.Contains("error", StringComparison.OrdinalIgnoreCase)
                       || line.Contains("failed", StringComparison.OrdinalIgnoreCase)
                       || line.Contains("Should", StringComparison.Ordinal))
        .Where(line => !line.TrimStart().StartsWith("0 Error", StringComparison.Ordinal))
        .Distinct()
        .Take(40);
    var tail = output.Split('\n').TakeLast(20);

    return $"""
        Vérification avant arrêt en échec : {label}.
        Corrige le problème avant de terminer (journal complet : {logFile}).

        {string.Join('\n', errorLines)}

        {string.Join('\n', tail)}
        """;
}

static string ReadOrEmpty(string path) => File.Exists(path) ? File.ReadAllText(path) : "";

static int Warn(string message)
{
    using var stdout = Console.OpenStandardOutput();
    using var json = new Utf8JsonWriter(stdout);
    json.WriteStartObject();
    json.WriteString("systemMessage", message);
    json.WriteEndObject();
    return 0;
}

static (int ExitCode, string Output) Run(
    string fileName, string arguments, IReadOnlyDictionary<string, string>? environment = null)
{
    var startInfo = new ProcessStartInfo(fileName, arguments)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
    startInfo.Environment["DOTNET_NOLOGO"] = "1";
    foreach (var (key, value) in environment ?? new Dictionary<string, string>())
        startInfo.Environment[key] = value;

    using var process = Process.Start(startInfo)!;
    var stdout = process.StandardOutput.ReadToEndAsync();
    var stderr = process.StandardError.ReadToEndAsync();
    process.WaitForExit();
    return (process.ExitCode, stdout.Result + stderr.Result);
}
