using System.Diagnostics;
using System.Text;

namespace AgoraFold.Admin;

public sealed class AdminTui(AdminUserService userService)
{
    public async Task<int> RunAsync()
    {
        var selectedIndex = 0;

        while (true)
        {
            var users = await userService.ListAsync();
            var itemCount = users.Count + 1;
            selectedIndex = Math.Clamp(selectedIndex, 0, itemCount - 1);

            ConsoleKey key;
            do
            {
                RenderUsers(users, selectedIndex);
                key = ReadNavigationKey();
                selectedIndex = MoveSelection(key, selectedIndex, itemCount);
                if (NumberFromKey(key) is not null)
                {
                    key = ConsoleKey.Enter;
                }
            }
            while (key is not (ConsoleKey.Enter or ConsoleKey.Escape or ConsoleKey.Q));

            if (key is ConsoleKey.Escape or ConsoleKey.Q)
            {
                return 0;
            }

            if (selectedIndex == users.Count)
            {
                await AddAsync();
            }
            else
            {
                await UserActionsAsync(users[selectedIndex]);
            }
        }
    }

    public static void PrintUsage()
    {
        Console.WriteLine("AgoraFold.Admin.Tui");
        Console.WriteLine();
        Console.WriteLine("Interactive user administration. Use Up/Down to highlight, number keys to open immediately, Enter to open the highlighted row, and Esc/Q to exit.");
        Console.WriteLine();
        Console.WriteLine("Run with:");
        Console.WriteLine("  dotnet run --project tools/AgoraFold.Admin.Tui");
    }

    private static int MoveSelection(ConsoleKey key, int selectedIndex, int itemCount)
    {
        if (key == ConsoleKey.UpArrow)
        {
            return selectedIndex == 0 ? itemCount - 1 : selectedIndex - 1;
        }

        if (key == ConsoleKey.DownArrow)
        {
            return selectedIndex == itemCount - 1 ? 0 : selectedIndex + 1;
        }

        var number = NumberFromKey(key);
        if (number is null)
        {
            return selectedIndex;
        }

        var numberIndex = number.Value == 0 ? itemCount - 1 : number.Value - 1;
        return numberIndex < itemCount ? numberIndex : selectedIndex;
    }

    private static void RenderUsers(IReadOnlyList<AdminUserSummary> users, int selectedIndex)
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }

        Console.WriteLine("AgoraFold Admin");
        Console.WriteLine("===============");
        Console.WriteLine("Users (Up/Down to highlight, number keys to open immediately, Enter to open, Esc/Q to exit)");
        Console.WriteLine();

        if (users.Count == 0)
        {
            Console.WriteLine("  No users found.");
        }

        for (var i = 0; i < users.Count; i++)
        {
            var user = users[i];
            WriteUserRow(i == selectedIndex, i + 1, user);
        }

        var addNumber = users.Count + 1 == 10 ? 0 : users.Count + 1;
        WriteRow(selectedIndex == users.Count, $"{addNumber}. + Add user");
    }

    private static void WriteUserRow(bool selected, int number, AdminUserSummary user)
    {
        var useColor = !Console.IsOutputRedirected;
        var foreground = Console.ForegroundColor;
        var background = Console.BackgroundColor;
        if (selected && useColor)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkGray;
        }

        Console.Write($"{(selected ? ">" : " ")} {(number == 10 ? 0 : number)}. {user.DisplayName} <{user.Email}> ");
        if (useColor)
        {
            Console.ForegroundColor = user.IsActive ? ConsoleColor.Green : ConsoleColor.Red;
        }

        Console.Write(user.IsActive ? "[active]" : "[inactive]");
        if (useColor)
        {
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
        }

        Console.WriteLine();
    }

    private static void WriteRow(bool selected, string text)
    {
        var useColor = !Console.IsOutputRedirected;
        var foreground = Console.ForegroundColor;
        var background = Console.BackgroundColor;
        if (selected && useColor)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkGray;
        }

        Console.WriteLine($"{(selected ? ">" : " ")} {text}");
        if (useColor)
        {
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
        }
    }

    private static void WriteStatus(bool isActive)
    {
        var useColor = !Console.IsOutputRedirected;
        Console.Write("Status: ");
        if (useColor)
        {
            Console.ForegroundColor = isActive ? ConsoleColor.Green : ConsoleColor.Red;
        }

        Console.WriteLine(isActive ? "active" : "inactive");
        if (useColor)
        {
            Console.ResetColor();
        }
    }

    private async Task UserActionsAsync(AdminUserSummary user)
    {
        var actions = 5;
        var selectedIndex = 0;
        while (true)
        {
            ClearScreen();
            Console.WriteLine($"User: {user.DisplayName} <{user.Email}>");
            WriteStatus(user.IsActive);
            Console.WriteLine();
            WriteRow(selectedIndex == 0, "1. Activate user");
            WriteRow(selectedIndex == 1, "2. Deactivate user");
            WriteRow(selectedIndex == 2, "3. Set password");
            WriteRow(selectedIndex == 3, "4. Delete user");
            WriteRow(selectedIndex == 4, "0. Back");
            Console.WriteLine();

            Console.WriteLine("Use Up/Down to highlight, number keys to run immediately, Enter to run, Esc to go back.");
            var key = ReadNavigationKey();
            selectedIndex = MoveSelection(key, selectedIndex, actions);
            if (NumberFromKey(key) is not null)
            {
                key = ConsoleKey.Enter;
            }
            if (key is ConsoleKey.Escape or ConsoleKey.Q)
            {
                return;
            }

            if (key != ConsoleKey.Enter)
            {
                continue;
            }

            switch (selectedIndex)
            {
                case 0:
                    var activateResult = await userService.SetActiveAsync(user.Email, active: true);
                    ShowResult(activateResult);
                    if (activateResult.Succeeded)
                    {
                        user = user with { IsActive = true };
                    }

                    break;
                case 1:
                    var deactivateResult = await userService.SetActiveAsync(user.Email, active: false);
                    ShowResult(deactivateResult);
                    if (deactivateResult.Succeeded)
                    {
                        user = user with { IsActive = false };
                    }

                    break;
                case 2:
                    ShowResult(await userService.SetPasswordAsync(user.Email, ReadPassword("New password: ")));
                    break;
                case 3:
                    if (ConfirmDelete(user.Email))
                    {
                        ShowResult(await userService.DeleteAsync(user.Email));
                        return;
                    }

                    ShowMessage("Deletion cancelled.");
                    break;
                case 4:
                    return;
            }
        }
    }

    private async Task AddAsync()
    {
        ClearScreen();
        Console.WriteLine("Add user");
        Console.WriteLine("========");
        Console.WriteLine();
        var email = ReadRequired("Email: ");
        var displayName = ReadRequired("Display name: ");
        var password = ReadPassword("Password: ");
        ShowResult(await userService.AddAsync(email, displayName, password));
    }

    private static bool ConfirmDelete(string email)
    {
        Console.Write($"Type '{email}' to confirm deletion: ");
        return string.Equals(Console.ReadLine(), email, StringComparison.Ordinal);
    }

    private static string ReadRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var value = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            Console.WriteLine("A value is required.");
        }
    }

    private static string ReadPassword(string prompt)
    {
        Console.Write(prompt);
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine() ?? "";
        }

        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return password.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length -= 1;
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
            }
        }
    }

    private static void ShowResult(AdminOperationResult result) => ShowMessage(result.Message);

    private static void ShowMessage(string message)
    {
        Console.WriteLine();
        Console.WriteLine(message);
        Console.WriteLine();
        var foreground = Console.ForegroundColor;
        if (!Console.IsOutputRedirected)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
        }

        Console.Write("Press Enter to continue...");
        if (!Console.IsOutputRedirected)
        {
            Console.ForegroundColor = foreground;
        }

        Console.ReadLine();
    }

    private static ConsoleKey ReadNavigationKey()
    {
        if (!Console.IsInputRedirected)
        {
            return ReadTerminalKey();
        }

        return Console.ReadLine()?.Trim().ToLowerInvariant() switch
        {
            "up" => ConsoleKey.UpArrow,
            "down" => ConsoleKey.DownArrow,
            "enter" => ConsoleKey.Enter,
            "0" => ConsoleKey.D0,
            "q" or "quit" => ConsoleKey.Q,
            _ => ConsoleKey.NoName,
        };
    }

    private static int? NumberFromKey(ConsoleKey key) => key switch
    {
        ConsoleKey.D0 or ConsoleKey.NumPad0 => 0,
        ConsoleKey.D1 or ConsoleKey.NumPad1 => 1,
        ConsoleKey.D2 or ConsoleKey.NumPad2 => 2,
        ConsoleKey.D3 or ConsoleKey.NumPad3 => 3,
        ConsoleKey.D4 or ConsoleKey.NumPad4 => 4,
        ConsoleKey.D5 or ConsoleKey.NumPad5 => 5,
        ConsoleKey.D6 or ConsoleKey.NumPad6 => 6,
        ConsoleKey.D7 or ConsoleKey.NumPad7 => 7,
        ConsoleKey.D8 or ConsoleKey.NumPad8 => 8,
        ConsoleKey.D9 or ConsoleKey.NumPad9 => 9,
        _ => null,
    };

    private static ConsoleKey ReadTerminalKey()
    {
        var first = Console.ReadKey(intercept: true);
        if (first.Key != ConsoleKey.Escape)
        {
            return first.Key;
        }

        // ConEmu can expose arrow keys as ANSI escape sequences (ESC [ A/B)
        // instead of ConsoleKey.UpArrow/DownArrow.
        var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 20;
        while (!Console.KeyAvailable && Stopwatch.GetTimestamp() < deadline)
        {
            Thread.Yield();
        }

        if (!Console.KeyAvailable)
        {
            return ConsoleKey.Escape;
        }

        var prefix = Console.ReadKey(intercept: true);
        if (prefix.KeyChar is not ('[' or 'O'))
        {
            return ConsoleKey.Escape;
        }

        var sequenceDeadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 20;
        while (!Console.KeyAvailable && Stopwatch.GetTimestamp() < sequenceDeadline)
        {
            Thread.Yield();
        }

        if (!Console.KeyAvailable)
        {
            return ConsoleKey.Escape;
        }

        return Console.ReadKey(intercept: true).KeyChar switch
        {
            'A' => ConsoleKey.UpArrow,
            'B' => ConsoleKey.DownArrow,
            _ => ConsoleKey.Escape,
        };
    }

    private static void ClearScreen()
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }
    }
}
