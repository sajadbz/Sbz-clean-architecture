using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sbz.Application.Accounts;

public sealed record ManagedAccount(string Id, string UserName, bool IsAdministrator, bool IsDisabled, bool IsLocked);
public sealed record AccountCreation(AccountResult Result, IReadOnlyList<string> Errors);
public sealed record AccountPage(IReadOnlyList<ManagedAccount> Accounts, int Total);

public enum AccountAction { Promote, Demote, Disable, Enable, Unlock, Delete }
public enum AccountResult { Saved, NotFound, Forbidden, Invalid }

public interface IAccountAdministration
{
    Task<AccountPage> ListAsync(string search, int page, CancellationToken cancellationToken);
    Task<ManagedAccount> FindAsync(string id, CancellationToken cancellationToken);
    Task<AccountCreation> CreateAsync(string actorId, string userName, string password, CancellationToken cancellationToken);
    Task<AccountResult> ChangeAsync(string actorId, string targetId, AccountAction action, CancellationToken cancellationToken);
}
