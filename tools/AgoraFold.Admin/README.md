# AgoraFold.Admin

Small local-development CLI for ASP.NET Identity user administration. Postgres must be running first:

```text
docker compose up -d
```

Commands:

```text
dotnet run --project tools/AgoraFold.Admin -- user add --email <email> --display-name <name> --password <password>
dotnet run --project tools/AgoraFold.Admin -- user activate --email <email>
dotnet run --project tools/AgoraFold.Admin -- user deactivate --email <email>
dotnet run --project tools/AgoraFold.Admin -- user delete --email <email>
dotnet run --project tools/AgoraFold.Admin -- user set-password --email <email> --password <password>
```

Deactivation uses ASP.NET Identity lockout state and updates the security stamp, so new logins are rejected and existing cookie sessions are invalidated when security-stamp validation runs. Deleting a user with listings or conversations may be rejected by the domain's restrictive foreign keys.

The default connection string is in `appsettings.json`. Override it with the standard `ConnectionStrings__Default` environment variable.
