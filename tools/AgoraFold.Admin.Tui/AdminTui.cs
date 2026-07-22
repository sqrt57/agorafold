using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;
using Color = Terminal.Gui.Drawing.Color;
using ColorName16 = Terminal.Gui.Drawing.ColorName16;
using Scheme = Terminal.Gui.Drawing.Scheme;
using VisualRole = Terminal.Gui.Drawing.VisualRole;

namespace AgoraFold.Admin;

public sealed class AdminTui(AdminUserService userService)
{
    private IApplication _app = null!;
    private Window _window = null!;
    private ListView _activeList = null!;
    private Action _activateCurrentSelection = null!;
    private int _activeItemCount;
    private bool _showingUserActions;
    private bool _modalOpen;
    private IReadOnlyList<AdminUserSummary> _users = [];
    private bool _busy;

    public async Task<int> RunAsync()
    {
        using var app = Application.Create();
        app.Init();
        _app = app;
        _app.Keyboard.KeyDown += HandleApplicationKeyDown;
        _users = await userService.ListAsync();

        _window = new Window
        {
            Title = "AgoraFold Admin",
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        ShowMainScreen();
        await app.RunAsync(_window, CancellationToken.None, null);
        return 0;
    }

    public static void PrintUsage()
    {
        Console.WriteLine("AgoraFold.Admin.Tui");
        Console.WriteLine();
        Console.WriteLine("Interactive user administration powered by Terminal.Gui.");
        Console.WriteLine("Use Up/Down to navigate, number keys to run items immediately, Enter to run the highlighted item, and Esc/Q to go back or exit.");
        Console.WriteLine();
        Console.WriteLine("Run with:");
        Console.WriteLine("  dotnet run --project tools/AgoraFold.Admin.Tui");
    }

    private void ShowMainScreen()
    {
        _window.RemoveAll();

        var list = new ListView
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(1),
            Height = Dim.Fill(2),
        };
        list.Source = new UserListSource(_users);
        list.Accepted += (_, _) => _ = ExecuteMainSelectionAsync(list);
        list.KeyDown += (_, key) => HandleMenuKey(key, list, _users.Count + 1, () => _ = ExecuteMainSelectionAsync(list), isMainMenu: true);
        _activeList = list;
        _activateCurrentSelection = () => _ = ExecuteMainSelectionAsync(list);
        _activeItemCount = _users.Count + 1;
        _showingUserActions = false;

        _window.Add(
            new Label
            {
                Text = "Users",
                X = 1,
                Y = 0,
                Width = Dim.Fill(1),
            },
            new Label
            {
                Text = "Up/Down: navigate   Number: run immediately   Enter: run   Esc/Q: exit",
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
            },
            list,
            new Label
            {
                Text = "Select + Add user to create an account.",
                X = 1,
                Y = Pos.AnchorEnd(1),
                Width = Dim.Fill(1),
            });
    }

    private async Task ExecuteMainSelectionAsync(ListView list)
    {
        if (_busy || list.SelectedItem is not int index || index < 0)
        {
            return;
        }

        _busy = true;
        try
        {
            if (index == _users.Count)
            {
                await AddUserAsync();
            }
            else if (index < _users.Count)
            {
                ShowUserActions(_users[index]);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private void ShowUserActions(AdminUserSummary user)
    {
        _window.RemoveAll();

        var actions = new ObservableCollection<string>
        {
            "1. Activate user",
            "2. Deactivate user",
            "3. Set password",
            "4. Delete user",
        };
        var list = new ListView
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill(1),
            Height = Dim.Fill(2),
        };
        list.SetSource(actions);
        list.Accepted += (_, _) => _ = ExecuteUserActionAsync(user, list);
        list.KeyDown += (_, key) => HandleMenuKey(key, list, actions.Count, () => _ = ExecuteUserActionAsync(user, list), isMainMenu: false);
        _activeList = list;
        _activeItemCount = actions.Count;
        _activateCurrentSelection = () => _ = ExecuteUserActionAsync(user, list);
        _showingUserActions = true;

        var statusPrefix = new Label
        {
            Text = "Status: ",
            X = 1,
            Y = 1,
        };
        var statusValue = new Label
        {
            Text = user.IsActive ? "active" : "inactive",
            X = Pos.Right(statusPrefix),
            Y = 1,
        };
        statusValue.SetScheme(new Scheme(StatusAttribute(_window, user.IsActive)));

        _window.Add(
            new Label
            {
                Text = $"User: {user.DisplayName} <{user.Email}>",
                X = 1,
                Y = 0,
                Width = Dim.Fill(1),
            },
            statusPrefix,
            statusValue,
            new Label
            {
                Text = "User actions",
                X = 1,
                Y = 3,
                Width = Dim.Fill(1),
            },
            list,
            new Label
            {
                Text = "Up/Down: navigate   Number: run immediately   Enter: run   Esc/Q: back",
                X = 1,
                Y = Pos.AnchorEnd(1),
                Width = Dim.Fill(1),
            });
    }

    private static Attribute StatusAttribute(View view, bool isActive)
    {
        var background = view.GetAttributeForRole(VisualRole.Normal).Background;
        var foreground = new Color(isActive ? ColorName16.BrightGreen : ColorName16.BrightRed);
        return new Attribute(foreground, background);
    }

    private async Task ExecuteUserActionAsync(AdminUserSummary user, ListView list)
    {
        if (_busy || list.SelectedItem is not int index || index < 0)
        {
            return;
        }

        _busy = true;
        try
        {
            switch (index)
            {
                case 0:
                    await SetActiveAsync(user, active: true);
                    break;
                case 1:
                    await SetActiveAsync(user, active: false);
                    break;
                case 2:
                    await SetPasswordAsync(user);
                    break;
                case 3:
                    await DeleteUserAsync(user);
                    break;
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task SetActiveAsync(AdminUserSummary user, bool active)
    {
        var result = await userService.SetActiveAsync(user.Email, active);
        ShowResult(result);
        if (result.Succeeded)
        {
            ShowUserActions(user with { IsActive = active });
        }
    }

    private async Task AddUserAsync()
    {
        var values = ShowForm(
            "Add user",
            new FormField("Email", false),
            new FormField("Display name", false),
            new FormField("Password", true));
        if (values is null)
        {
            return;
        }

        var result = await userService.AddAsync(values[0], values[1], values[2]);
        ShowResult(result);
        if (result.Succeeded)
        {
            await RefreshUsersAsync();
        }
    }

    private async Task SetPasswordAsync(AdminUserSummary user)
    {
        var values = ShowForm("Set password", new FormField("New password", true));
        if (values is null)
        {
            return;
        }

        var result = await userService.SetPasswordAsync(user.Email, values[0]);
        ShowResult(result);
        ShowUserActions(user);
    }

    private async Task DeleteUserAsync(AdminUserSummary user)
    {
        var values = ShowForm("Delete user", new FormField($"Type {user.Email} to confirm", false));
        if (values is null)
        {
            return;
        }

        if (!string.Equals(values[0], user.Email, StringComparison.Ordinal))
        {
            ShowResult(new AdminOperationResult(false, "The confirmation text did not match the email address."));
            ShowUserActions(user);
            return;
        }

        var result = await userService.DeleteAsync(user.Email);
        ShowResult(result);
        if (result.Succeeded)
        {
            await RefreshUsersAsync();
        }
        else
        {
            ShowUserActions(user);
        }
    }

    private async Task RefreshUsersAsync()
    {
        _users = await userService.ListAsync();
        ShowMainScreen();
    }

    private string[]? ShowForm(string title, params FormField[] fields)
    {
        var dialog = new Dialog
        {
            Title = title,
            Width = 72,
            Height = Math.Max(8, fields.Length * 2 + 5),
        };
        var textFields = new List<TextField>();

        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            dialog.Add(new Label
            {
                Text = field.Label,
                X = 1,
                Y = i * 2 + 1,
                Width = 25,
            });

            var textField = new TextField
            {
                X = 27,
                Y = i * 2 + 1,
                Width = Dim.Fill(2),
                Secret = field.Secret,
            };
            dialog.Add(textField);
            textFields.Add(textField);
        }

        dialog.AddButton(new Button { Text = "_Cancel" });
        dialog.AddButton(new Button { Text = "_OK", IsDefault = true });
        _modalOpen = true;
        try
        {
            _app.Run(dialog);
        }
        finally
        {
            _modalOpen = false;
        }

        if (dialog.Result != 1)
        {
            return null;
        }

        return textFields.Select(field => field.Text?.Trim() ?? string.Empty).ToArray();
    }

    private void ShowResult(AdminOperationResult result)
    {
        _modalOpen = true;
        try
        {
            MessageBox.Query(_app, result.Succeeded ? "Success" : "Operation failed", result.Message, "_OK");
        }
        finally
        {
            _modalOpen = false;
        }
    }

    private void HandleApplicationKeyDown(object? sender, Key key)
    {
        if (_modalOpen)
        {
            return;
        }

        HandleMenuKey(key, _activeList, _activeItemCount, _activateCurrentSelection, !_showingUserActions);
    }

    private void HandleMenuKey(Key key, ListView list, int itemCount, Action activate, bool isMainMenu)
    {
        if (_modalOpen)
        {
            return;
        }

        if (IsEscape(key) || IsQuit(key))
        {
            key.Handled = true;
            if (isMainMenu)
            {
                _app.RequestStop();
            }
            else
            {
                ShowMainScreen();
            }

            return;
        }

        if (GetNumberIndex(key, itemCount) is not int index)
        {
            return;
        }

        key.Handled = true;
        list.SelectedItem = index;
        activate();
    }

    private static bool IsEscape(Key key) => key == Key.Esc || key.NoShift.NoCtrl.NoAlt.KeyCode == KeyCode.Esc;

    private static bool IsQuit(Key key) => key == Key.Q || key.NoShift.NoCtrl.NoAlt.KeyCode == KeyCode.Q;

    private static int? GetNumberIndex(Key key, int itemCount)
    {
        var number = key.NoShift.NoCtrl.NoAlt.KeyCode switch
        {
            KeyCode.D0 => 0,
            KeyCode.D1 => 1,
            KeyCode.D2 => 2,
            KeyCode.D3 => 3,
            KeyCode.D4 => 4,
            KeyCode.D5 => 5,
            KeyCode.D6 => 6,
            KeyCode.D7 => 7,
            KeyCode.D8 => 8,
            KeyCode.D9 => 9,
            _ => -1,
        };

        if (number < 0)
        {
            return null;
        }

        // "0" matches the 10th item's on-screen label (DisplayNumber wraps 10 to "0").
        var index = number == 0 ? 9 : number - 1;
        return index >= 0 && index < itemCount ? index : null;
    }

    private static string DisplayNumber(int number) => number == 10 ? "0" : number.ToString();

    private sealed record FormField(string Label, bool Secret);

    private sealed class UserListSource(IReadOnlyList<AdminUserSummary> users) : IListDataSource
    {
        // A fresh instance is built for each screen; the snapshot never mutates, so this is never raised.
#pragma warning disable CS0067
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
#pragma warning restore CS0067

        public int Count => users.Count + 1;

        public int MaxItemLength => Enumerable.Range(0, Count).Max(item => GetRow(item).Text.Length);

        public bool SuspendCollectionChangedEvent { get; set; }

        public bool IsMarked(int item) => false;

        public void SetMark(int item, bool value)
        {
        }

        public IList ToList() => Enumerable.Range(0, Count).Select(item => GetRow(item).Text).ToList();

        public bool RenderMark(ListView listView, int item, int row, bool isMarked, bool markMultiple) => false;

        public void Dispose()
        {
        }

        public void Render(ListView listView, bool selected, int item, int col, int row, int width, int start = 0)
        {
            var (text, statusStart, statusLength, isActive) = GetRow(item);
            var normalAttribute = listView.GetCurrentAttribute();
            var statusAttribute = StatusAttribute(listView, isActive);

            listView.Move(col, row);
            for (var i = 0; i < width; i++)
            {
                var charIndex = start + i;
                var inStatus = statusLength > 0 && charIndex >= statusStart && charIndex < statusStart + statusLength;
                listView.SetAttribute(inStatus ? statusAttribute : normalAttribute);
                listView.AddRune(charIndex < text.Length ? text[charIndex] : ' ');
            }
        }

        private (string Text, int StatusStart, int StatusLength, bool IsActive) GetRow(int item)
        {
            if (item == users.Count)
            {
                return ($"{DisplayNumber(item + 1)}. + Add user", 0, 0, false);
            }

            var user = users[item];
            var prefix = $"{DisplayNumber(item + 1)}. {user.DisplayName} <{user.Email}> [";
            var status = user.IsActive ? "active" : "inactive";
            return (prefix + status + "]", prefix.Length, status.Length, user.IsActive);
        }
    }
}
