using AgoraFold.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace AgoraFold.Admin;

public static class AdminCommand
{
    public static async Task<int> RunAsync(string[] args, UserManager<AppUser> userManager)
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
        return args[1].ToLowerInvariant() switch
        {
            "add" => await AddAsync(options, userManager),
            "activate" => await SetActiveAsync(options, userManager, active: true),
            "deactivate" => await SetActiveAsync(options, userManager, active: false),
            "delete" => await DeleteAsync(options, userManager),
            "set-password" => await SetPasswordAsync(options, userManager),
            _ => Fail($"Unknown user operation '{args[1]}'."),
        };
    }

    private static async Task<int> AddAsync(Dictionary<string, string> options, UserManager<AppUser> userManager)
    {
        var email = Required(options, "email");
        var displayName = Required(options, "display-name");
        var password = Required(options, "password");

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Fail($"A user with email '{email}' already exists.");
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true,
            LockoutEnabled = true,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return Fail(result);
        }

        Console.WriteLine($"Created user {user.Email} ({user.Id}).");
        return 0;
    }

    private static async Task<int> SetActiveAsync(
        Dictionary<string, string> options,
        UserManager<AppUser> userManager,
        bool active)
    {
        var user = await FindUserAsync(options, userManager);
        if (user is null)
        {
            return 1;
        }

        var lockoutEnabledResult = await userManager.SetLockoutEnabledAsync(user, true);
        if (!lockoutEnabledResult.Succeeded)
        {
            return Fail(lockoutEnabledResult);
        }

        var lockoutEndResult = await userManager.SetLockoutEndDateAsync(
            user,
            active ? null : DateTimeOffset.MaxValue);
        if (!lockoutEndResult.Succeeded)
        {
            return Fail(lockoutEndResult);
        }

        if (active)
        {
            var resetFailedAccessResult = await userManager.ResetAccessFailedCountAsync(user);
            if (!resetFailedAccessResult.Succeeded)
            {
                return Fail(resetFailedAccessResult);
            }
        }

        var securityStampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!securityStampResult.Succeeded)
        {
            return Fail(securityStampResult);
        }

        Console.WriteLine(active
            ? $"Activated user {user.Email}."
            : $"Deactivated user {user.Email}.");
        return 0;
    }

    private static async Task<int> DeleteAsync(Dictionary<string, string> options, UserManager<AppUser> userManager)
    {
        var user = await FindUserAsync(options, userManager);
        if (user is null)
        {
            return 1;
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return Fail(result);
        }

        Console.WriteLine($"Deleted user {user.Email}.");
        return 0;
    }

    private static async Task<int> SetPasswordAsync(
        Dictionary<string, string> options,
        UserManager<AppUser> userManager)
    {
        var user = await FindUserAsync(options, userManager);
        if (user is null)
        {
            return 1;
        }

        var password = Required(options, "password");
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            return Fail(result);
        }

        Console.WriteLine($"Updated password for {user.Email}.");
        return 0;
    }

    private static async Task<AppUser?> FindUserAsync(
        Dictionary<string, string> options,
        UserManager<AppUser> userManager)
    {
        var email = Required(options, "email");
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            Console.Error.WriteLine($"No user found with email '{email}'.");
        }

        return user;
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

    private static int Fail(IdentityResult result) =>
        Fail(string.Join(Environment.NewLine, result.Errors.Select(error => error.Description)));

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
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
}
