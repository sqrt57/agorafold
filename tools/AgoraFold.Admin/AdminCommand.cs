namespace AgoraFold.Admin;

public static class AdminCommand
{
    public static async Task<int> RunAsync(string[] args, AdminUserService userService)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return 0;
        }

        if (!string.Equals(args[0], "user", StringComparison.OrdinalIgnoreCase) || args.Length < 2)
        {
            return Fail("Expected a user operation.");
        }

        if (IsHelp(args[1]))
        {
            PrintUsage();
            return 0;
        }

        var options = ParseOptions(args[2..]);
        var result = args[1].ToLowerInvariant() switch
        {
            "add" => await userService.AddAsync(
                Required(options, "email"),
                Required(options, "display-name"),
                Required(options, "password")),
            "activate" => await userService.SetActiveAsync(Required(options, "email"), active: true),
            "deactivate" => await userService.SetActiveAsync(Required(options, "email"), active: false),
            "delete" => await userService.DeleteAsync(Required(options, "email")),
            "set-password" => await userService.SetPasswordAsync(
                Required(options, "email"),
                Required(options, "password")),
            _ => new AdminOperationResult(false, $"Unknown user operation '{args[1]}'."),
        };

        return PrintResult(result);
    }

    public static void PrintUsage()
    {
        Console.WriteLine("AgoraFold.Admin");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tools/AgoraFold.Admin -- user add --email <email> --display-name <name> --password <password>");
        Console.WriteLine("  dotnet run --project tools/AgoraFold.Admin -- user activate --email <email>");
        Console.WriteLine("  dotnet run --project tools/AgoraFold.Admin -- user deactivate --email <email>");
        Console.WriteLine("  dotnet run --project tools/AgoraFold.Admin -- user delete --email <email>");
        Console.WriteLine("  dotnet run --project tools/AgoraFold.Admin -- user set-password --email <email> --password <password>");
        Console.WriteLine();
        Console.WriteLine("The database connection comes from tools/AgoraFold.Admin/appsettings.json");
        Console.WriteLine("and can be overridden with ConnectionStrings__Default.");
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var name = args[i];
            if (!name.StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Length)
            {
                throw new ArgumentException($"Expected an option followed by a value, got '{name}'.");
            }

            options[name[2..]] = args[++i];
        }

        return options;
    }

    private static string Required(Dictionary<string, string> options, string name)
    {
        if (!options.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Missing required option '--{name}'.");
        }

        return value;
    }

    private static bool IsHelp(string value) => value is "-h" or "--help";

    private static int PrintResult(AdminOperationResult result)
    {
        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            return 0;
        }

        return Fail(result.Message);
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
