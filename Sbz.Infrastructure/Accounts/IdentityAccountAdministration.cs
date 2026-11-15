using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sbz.Application.Accounts;
using Sbz.Infrastructure.Persistence;

namespace Sbz.Infrastructure.Accounts;

internal sealed class IdentityAccountAdministration(ApplicationDbContext db, UserManager<IdentityUser> users)
    : IAccountAdministration
{
    private const int PageSize = 25;
    private static readonly DateTimeOffset DisabledUntil = DateTimeOffset.MaxValue;

    public Task<int> CountAsync(CancellationToken cancellationToken) => db.Users.CountAsync(cancellationToken);

    public async Task<AccountPage> ListAsync(string search, int page, CancellationToken cancellationToken)
    {
        var accounts = users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = users.NormalizeName(search.Trim());
            accounts = accounts.Where(user => user.NormalizedUserName.Contains(normalized));
        }

        var total = await accounts.CountAsync(cancellationToken);
        var selected = await accounts.OrderBy(user => user.NormalizedUserName)
            .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(cancellationToken);
        var ids = selected.Select(user => user.Id).ToArray();
        var adminIds = await (from membership in db.UserRoles
                              join role in db.Roles on membership.RoleId equals role.Id
                              where role.Name == AccountRoles.Administrator && ids.Contains(membership.UserId)
                              select membership.UserId).ToListAsync(cancellationToken);

        return new AccountPage(selected.Select(user => Describe(user, adminIds.Contains(user.Id))).ToArray(), total);
    }

    public async Task<ManagedAccount> FindAsync(string id, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return user is null ? null : Describe(user, await users.IsInRoleAsync(user, AccountRoles.Administrator));
    }

    public async Task<AccountResult> ChangeAsync(
        string actorId, string targetId, AccountAction action, CancellationToken cancellationToken)
    {
        if (actorId == targetId)
            return AccountResult.Forbidden;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await IsAdministratorAsync(actorId))
            return AccountResult.Forbidden;

        var target = await users.FindByIdAsync(targetId);
        if (target is null)
            return AccountResult.NotFound;

        var result = await ApplyAsync(target, action);
        if (!result.Succeeded)
            return AccountResult.Invalid;

        if (action != AccountAction.Delete && !(await users.UpdateSecurityStampAsync(target)).Succeeded)
            return AccountResult.Invalid;

        await transaction.CommitAsync(cancellationToken);
        return AccountResult.Saved;
    }

    public async Task<AccountCreation> CreateAsync(
        string actorId, string userName, string password, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await IsAdministratorAsync(actorId))
            return new AccountCreation(AccountResult.Forbidden, []);

        var result = await users.CreateAsync(new IdentityUser(userName), password);
        if (!result.Succeeded)
            return new AccountCreation(AccountResult.Invalid, result.Errors.Select(error => error.Description).ToArray());

        await transaction.CommitAsync(cancellationToken);
        return new AccountCreation(AccountResult.Saved, []);
    }

    private async Task<bool> IsAdministratorAsync(string id)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(account => account.Id == id);
        return user is not null && !IsDisabled(user) && await users.IsInRoleAsync(user, AccountRoles.Administrator);
    }

    private async Task<IdentityResult> ApplyAsync(IdentityUser user, AccountAction action)
    {
        switch (action)
        {
            case AccountAction.Promote:
                return await users.IsInRoleAsync(user, AccountRoles.Administrator)
                    ? IdentityResult.Success
                    : await users.AddToRoleAsync(user, AccountRoles.Administrator);
            case AccountAction.Demote:
                return await users.IsInRoleAsync(user, AccountRoles.Administrator)
                    ? await users.RemoveFromRoleAsync(user, AccountRoles.Administrator)
                    : IdentityResult.Success;
            case AccountAction.Disable:
                return await users.SetLockoutEndDateAsync(user, DisabledUntil);
            case AccountAction.Enable:
            case AccountAction.Unlock:
                if ((action == AccountAction.Enable) != IsDisabled(user))
                    return IdentityResult.Failed();

                var unlocked = await users.SetLockoutEndDateAsync(user, null);
                return unlocked.Succeeded ? await users.ResetAccessFailedCountAsync(user) : unlocked;
            case AccountAction.Delete:
                return await users.DeleteAsync(user);
            default:
                return IdentityResult.Failed();
        }
    }

    private static ManagedAccount Describe(IdentityUser user, bool administrator)
        => new(user.Id, user.UserName, administrator, IsDisabled(user), user.LockoutEnd > DateTimeOffset.UtcNow);

    private static bool IsDisabled(IdentityUser user) => user.LockoutEnd == DisabledUntil;
}
