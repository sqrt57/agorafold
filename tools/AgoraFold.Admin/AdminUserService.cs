using AgoraFold.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgoraFold.Admin;

public sealed class AdminUserService(UserManager<AppUser> userManager)
{
    public async Task<AdminOperationResult> AddAsync(string email, string displayName, string password)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Failure($"A user with email '{email}' already exists.");
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
        return result.Succeeded
            ? Success($"Created user {user.Email} ({user.Id}).")
            : Failure(result);
    }

    public async Task<AdminOperationResult> SetActiveAsync(string email, bool active)
    {
        var user = await FindAsync(email);
        if (user is null)
        {
            return Failure($"No user found with email '{email}'.");
        }

        var lockoutEnabledResult = await userManager.SetLockoutEnabledAsync(user, true);
        if (!lockoutEnabledResult.Succeeded)
        {
            return Failure(lockoutEnabledResult);
        }

        var lockoutEndResult = await userManager.SetLockoutEndDateAsync(
            user,
            active ? null : DateTimeOffset.MaxValue);
        if (!lockoutEndResult.Succeeded)
        {
            return Failure(lockoutEndResult);
        }

        if (active)
        {
            var resetFailedAccessResult = await userManager.ResetAccessFailedCountAsync(user);
            if (!resetFailedAccessResult.Succeeded)
            {
                return Failure(resetFailedAccessResult);
            }
        }

        var securityStampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!securityStampResult.Succeeded)
        {
            return Failure(securityStampResult);
        }

        return Success(active
            ? $"Activated user {user.Email}."
            : $"Deactivated user {user.Email}.");
    }

    public async Task<AdminOperationResult> DeleteAsync(string email)
    {
        var user = await FindAsync(email);
        if (user is null)
        {
            return Failure($"No user found with email '{email}'.");
        }

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded
            ? Success($"Deleted user {user.Email}.")
            : Failure(result);
    }

    public async Task<AdminOperationResult> SetPasswordAsync(string email, string password)
    {
        var user = await FindAsync(email);
        if (user is null)
        {
            return Failure($"No user found with email '{email}'.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, password);
        return result.Succeeded
            ? Success($"Updated password for {user.Email}.")
            : Failure(result);
    }

    public Task<List<AdminUserSummary>> ListAsync(CancellationToken cancellationToken = default) =>
        userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new AdminUserSummary(
                user.Id,
                user.Email ?? user.UserName ?? "(no email)",
                user.DisplayName,
                user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow))
            .ToListAsync(cancellationToken);

    private Task<AppUser?> FindAsync(string email) => userManager.FindByEmailAsync(email);

    private static AdminOperationResult Success(string message) => new(true, message);

    private static AdminOperationResult Failure(IdentityResult result) =>
        Failure(string.Join(Environment.NewLine, result.Errors.Select(error => error.Description)));

    private static AdminOperationResult Failure(string message) => new(false, message);
}

public sealed record AdminOperationResult(bool Succeeded, string Message);

public sealed record AdminUserSummary(string Id, string Email, string DisplayName, bool IsActive);
