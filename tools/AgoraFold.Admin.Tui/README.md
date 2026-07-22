# AgoraFold.Admin.Tui

Interactive terminal UI for the same local Identity administration operations as `AgoraFold.Admin`.

```text
docker compose up -d
dotnet run --project tools/AgoraFold.Admin.Tui
```

The main screen lists users with the selected row highlighted. Use Up/Down to move, number keys to open a row immediately, and Enter to open the highlighted row or `Add user`; Esc/Q exits. Password input is hidden, and deletion requires typing the email address to confirm.
